namespace RecipeApp.Domain.Entities;

public class ExternalLogin
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderUserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}