using RecipeApp.Domain.Entities;

namespace RecipeApp.Application.Recipes.Interface;

public interface IRecipeRepository
{
    Task AddAsync(Recipe recipe);
        
    Task RemoveAsync(Recipe recipe);

    Task<Recipe?> GetByIdAsync(int recipeId);
    Task<IEnumerable<Recipe>> GetAllByUserIdAsync(int userId);
    Task SaveChangesAsync();
    void RemoveIngredients(IEnumerable<Ingredient> ingredients);
    void RemovePrepareSteps(IEnumerable<PrepareStep> prepareSteps);
}