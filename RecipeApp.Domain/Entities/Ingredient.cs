namespace RecipeApp.Domain.Entities;

public class Ingredient
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public short Position { get; set; }

    public Recipe Recipe { get; set; } = null!;
}