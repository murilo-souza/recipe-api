using Microsoft.EntityFrameworkCore;
using RecipeApp.Application.Categories.Interface;
using RecipeApp.Domain.Entities;
using RecipeApp.Infrastructure.Persistence;


namespace RecipeApp.Infrastructure.Categories;

public class CategoryRepository: ICategoryRepository
{
    private readonly AppDbContext _db;

    public CategoryRepository(AppDbContext db)
    {
        _db = db;
    }
   
    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await _db.Categories.ToListAsync();
    }
}
