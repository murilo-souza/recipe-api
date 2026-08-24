using RecipeApp.Application.Chat.DTO;

namespace RecipeApp.Application.Chat.Interface;

public interface IGeneralChatService
{
    Task<GeneralChatMessageResponse> SendMessageAsync(int userId, GeneralChatMessageRequest request);
    Task<IEnumerable<GeneralChatMessageResponse>> GetHistoryAsync(int userId);
    Task ClearHistoryAsync(int userId);
}