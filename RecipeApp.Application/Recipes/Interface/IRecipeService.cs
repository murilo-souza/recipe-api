using RecipeApp.Application.Recipes.DTO;

namespace RecipeApp.Application.Recipes.Interface;

public interface IRecipeService
{
    Task<RecipeResponse> CreateRecipeAsync(int userId, CreateRecipeRequest request);
    Task UpdateRecipeAsync(int userId, int recipeId, UpdateRecipeRequest request);
    Task DeleteRecipeAsync(int userId, int recipeId);
    Task<IEnumerable<RecipeSummaryResponse>> GetAllRecipesAsync(int userId);
    Task<RecipeResponse?> GetRecipeByIdAsync(int userId, int recipeId);
}