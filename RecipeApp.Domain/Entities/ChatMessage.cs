namespace RecipeApp.Domain.Entities;

public enum ChatRole { User, Assistant }

public class ChatMessage
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public Recipe Recipe { get; set; } = null!;
}