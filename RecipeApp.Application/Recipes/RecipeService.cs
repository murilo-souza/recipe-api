using RecipeApp.Application.Common.Exceptions;
using RecipeApp.Application.Common.Interface;
using RecipeApp.Application.Recipes.DTO;
using RecipeApp.Application.Recipes.Interface;
using RecipeApp.Domain.Entities;

namespace RecipeApp.Application.Recipes;

public class RecipeService : IRecipeService
{
    private readonly IRecipeRepository _repository;
    private readonly IEmbeddingService _embeddingService;

    public RecipeService(IRecipeRepository repository, IEmbeddingService embeddingService)
    {
        _repository = repository;
        _embeddingService = embeddingService;
    }

    public async Task<IEnumerable<RecipeSummaryResponse>> GetAllRecipesAsync(int userId)
    {
        var recipes = await _repository.GetAllByUserIdAsync(userId);

        return recipes.Select(recipe => new RecipeSummaryResponse(
            recipe.Id,
            recipe.Title,
            recipe.Description ?? string.Empty,
            recipe.Image,
            recipe.CategoryId ?? 0,
            recipe.CreatedAt
        ));
    }

    public async Task<RecipeResponse?> GetRecipeByIdAsync(int userId, int recipeId)
    {
        var recipe = await _repository.GetByIdAsync(recipeId);
        if (recipe is null || recipe.UserId != userId)
            return null;

        return new RecipeResponse(
            recipe.Id,
            recipe.Title,
            recipe.Description ?? string.Empty,
            recipe.Image,
            recipe.CategoryId ?? 0,
            recipe.Ingredients.Select(i => i.Description).ToArray(),
            recipe.PrepareSteps
                .OrderBy(ps => ps.Position)
                .Select(ps =>new PrepareStepItem(ps.Id, ps.Description, ps.Position))
                .ToArray(),
            recipe.CreatedAt
        );
    }

    public async Task<RecipeResponse> CreateRecipeAsync(int userId, CreateRecipeRequest request)
    {
        var recipe = new Recipe
        {
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            CategoryId = request.CategoryId,
            Image = request.Image,
            Ingredients = request.Ingredients
                .Select(description => new Ingredient { Description = description })
                .ToList(),
            PrepareSteps = request.PrepareSteps
                .Select((description, index) => new PrepareStep
                {
                    Description = description,
                    Position = (short)(index + 1)
                })
                .ToList()
        };

        await TryAttachEmbeddingAsync(recipe);

        await _repository.AddAsync(recipe);
        await _repository.SaveChangesAsync();

        return new RecipeResponse(
            recipe.Id,
            recipe.Title,
            recipe.Description ?? string.Empty,
            recipe.Image,
            recipe.CategoryId ?? 0,
            recipe.Ingredients.Select(i => i.Description).ToArray(),
            recipe.PrepareSteps
                .OrderBy(ps => ps.Position)
                .Select(ps => new PrepareStepItem(ps.Id, ps.Description, ps.Position))
                .ToArray(),
            recipe.CreatedAt
        );
    }

    public async Task UpdateRecipeAsync(int userId, int recipeId, UpdateRecipeRequest request)
    {
        var recipe = await _repository.GetByIdAsync(recipeId);

        if (recipe is null)
            throw new RecipeNotFoundException();

        if (recipe.UserId != userId)
            throw new ForbiddenAccessException(); // ou uma exceção customizada, decide você

        recipe.Title = request.Title;
        recipe.Description = request.Description;
        recipe.CategoryId = request.CategoryId;
        recipe.Image = request.Image;

     
        _repository.RemoveIngredients(recipe.Ingredients);
        _repository.RemovePrepareSteps(recipe.PrepareSteps);


      
        recipe.Ingredients = request.Ingredients
            .Select(description => new Ingredient { Description = description })
            .ToList();

        recipe.PrepareSteps = request.PrepareSteps
            .Select((description, index) => new PrepareStep
            {
                Description = description,
                Position = (short)(index + 1)
            })
            .ToList();

        await TryAttachEmbeddingAsync(recipe);

        await _repository.SaveChangesAsync();
    }

    public async Task DeleteRecipeAsync(int userId, int recipeId)
    {
        var recipe = await _repository.GetByIdAsync(recipeId);

        if (recipe is null)
            throw new RecipeNotFoundException();

        if (recipe.UserId != userId)
            throw new ForbiddenAccessException();

        await _repository.RemoveAsync(recipe);
        await _repository.SaveChangesAsync();
    }

    public async Task<int> BackfillEmbeddingsAsync(int userId)
    {
        var recipes = await _repository.GetAllByUserIdWithDetailsAsync(userId); // precisa incluir Ingredients/PrepareSteps
        var updated = 0;

        foreach (var recipe in recipes.Where(r => r.Embedding is null))
        {
            await TryAttachEmbeddingAsync(recipe);
            if (recipe.Embedding is not null) updated++;
        }

        await _repository.SaveChangesAsync();
        return updated;
    }

    private async Task TryAttachEmbeddingAsync(Recipe recipe)
    {
        try
        {
            var text = BuildEmbeddingText(recipe);
            var values = await _embeddingService.GenerateEmbeddingAsync(text);

            if (values is not null)
                recipe.Embedding = new Pgvector.Vector(values);
        }
        catch
        {
            // Se o Gemini falhar (rate limit, fora do ar), não bloqueia o
            // Create/Update — a receita salva sem embedding, e pode ser
            // gerado depois via backfill
        }
    }

    private static string BuildEmbeddingText(Recipe recipe)
    {
        var ingredients = string.Join(", ", recipe.Ingredients.Select(i => i.Description));
        var steps = string.Join(". ", recipe.PrepareSteps.OrderBy(p => p.Position).Select(p => p.Description));

        return $"""
        Título: {recipe.Title}
        Descrição: {recipe.Description}
        Ingredientes: {ingredients}
        Modo de preparo: {steps}
        """;
    }
}