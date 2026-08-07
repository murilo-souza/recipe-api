using RecipeApp.Application.Common.Exceptions;
using RecipeApp.Application.Recipes.DTO;
using RecipeApp.Application.Recipes.Interface;
using RecipeApp.Domain.Entities;

namespace RecipeApp.Application.Recipes;

public class RecipeService : IRecipeService
{
    private readonly IRecipeRepository _repository;

    public RecipeService(IRecipeRepository repository)
    {
        _repository = repository;
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

    public async Task CreateRecipeAsync(int userId, CreateRecipeRequest request)
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

        await _repository.AddAsync(recipe);
        await _repository.SaveChangesAsync();

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
}