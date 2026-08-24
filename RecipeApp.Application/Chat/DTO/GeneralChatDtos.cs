namespace RecipeApp.Application.Chat.DTO;

public record GeneralChatMessageRequest(string Content);
public record GeneralChatMessageResponse(int Id, string Role, string Content, DateTimeOffset CreatedAt);