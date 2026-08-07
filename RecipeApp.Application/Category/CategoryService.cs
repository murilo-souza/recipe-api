using RecipeApp.Application.Categories.DTO;
using RecipeApp.Application.Categories.Interface;


namespace RecipeApp.Application.Categories
{
    public class CategoryService: ICategoryService
    {
        private readonly ICategoryRepository _repository;

        public CategoryService(ICategoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<CategoryResponse>> GetAllCategoriesAsync()
        {
            var categories = await _repository.GetAllAsync();

            return categories.Select(c => new CategoryResponse(c.Id, c.Name))
           ;
        }
    }
}
