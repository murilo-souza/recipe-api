using RecipeApp.Domain.Entities;

namespace RecipeApp.Application.Gemini.Interface
{
    public interface IGeminiService
    {
        Task<string> GenerateReplyAsync(Recipe recipe, IEnumerable<ChatMessage> chatHistory);
    }
}
