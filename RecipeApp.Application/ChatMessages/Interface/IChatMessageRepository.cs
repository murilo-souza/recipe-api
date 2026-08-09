using RecipeApp.Domain.Entities;

namespace RecipeApp.Application.ChatMessages.Interface;

public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage chatMessage);
    Task<IEnumerable<ChatMessage>> GetByRecipeIdAsync(int recipeId);
    Task<IEnumerable<ChatMessage>> GetRecentByRecipeIdAsync(int recipeId, int limit = 10);
    Task SaveChangesAsync();
}
