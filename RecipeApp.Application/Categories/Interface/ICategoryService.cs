using RecipeApp.Application.Categories.DTO;

namespace RecipeApp.Application.Categories.Interface;

public interface ICategoryService
{
    Task<IEnumerable<CategoryResponse>> GetAllCategoriesAsync();
}