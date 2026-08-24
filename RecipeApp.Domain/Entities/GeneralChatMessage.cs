namespace RecipeApp.Domain.Entities;

public class GeneralChatMessage
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}