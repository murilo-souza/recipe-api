using RecipeApp.Domain.Common;

namespace RecipeApp.Domain.Entities;

public class User : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();
    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}