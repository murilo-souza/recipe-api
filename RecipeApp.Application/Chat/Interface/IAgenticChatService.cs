using RecipeApp.Application.Chat.DTO;

namespace RecipeApp.Application.Chat.Interface
{
    public interface IAgenticChatService
    {
        Task<string> GenerateReplyAsync(int userId, IEnumerable<AgenticChatTurn> history, string userMessage);
    }
}
