using RecipeApp.Domain.Common;

namespace RecipeApp.Domain.Entities;

public class Recipe : IAuditable
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Image { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Category? Category { get; set; }
    public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
    public ICollection<PrepareStep> PrepareSteps { get; set; } = new List<PrepareStep>();
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}