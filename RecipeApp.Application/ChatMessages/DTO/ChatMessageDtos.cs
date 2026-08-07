namespace RecipeApp.Application.ChatMessages.DTO;

public record ChatMessageRequest(string Content);

public record ChatMessageResponse(int Id, string Role, string Content, int RecipeId, DateTimeOffset CreatedAt);