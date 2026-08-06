namespace RecipeApp.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}