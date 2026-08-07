using RecipeApp.Domain.Entities;

namespace RecipeApp.Application.Categories.Interface;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync();
}
