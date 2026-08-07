using Microsoft.EntityFrameworkCore;
using RecipeApp.Application.Recipes.Interface;
using RecipeApp.Domain.Entities;
using RecipeApp.Infrastructure.Persistence;

namespace RecipeApp.Infrastructure.Recipes;

public class RecipeRepository : IRecipeRepository
{
    private readonly AppDbContext _db;

    public RecipeRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Recipe recipe)
    {
        await _db.Recipes.AddAsync(recipe);
    }

    public async Task RemoveAsync(Recipe recipe)
    {
        _db.Recipes.Remove(recipe);
    }

    public async Task<Recipe?> GetByIdAsync(int recipeId)
    {
        return await _db.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.PrepareSteps)
            .SingleOrDefaultAsync(r => r.Id == recipeId);
    }

    public async Task<IEnumerable<Recipe>> GetAllByUserIdAsync(int userId)
    {
        return await _db.Recipes
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public void RemoveIngredients(IEnumerable<Ingredient> ingredients)
    {
        _db.Ingredients.RemoveRange(ingredients);
    }

    public void RemovePrepareSteps(IEnumerable<PrepareStep> prepareSteps)
    {
        _db.PrepareSteps.RemoveRange(prepareSteps);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}