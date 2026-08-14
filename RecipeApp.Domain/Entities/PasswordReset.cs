// RecipeApp.Domain/Entities/PasswordResetCode.cs
namespace RecipeApp.Domain.Entities;

public class PasswordResetCode
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;

    public bool IsValid => UsedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}