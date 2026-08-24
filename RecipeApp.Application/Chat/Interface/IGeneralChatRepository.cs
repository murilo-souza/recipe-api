using RecipeApp.Domain.Entities;

namespace RecipeApp.Application.Chat.Interface;

public interface IGeneralChatRepository
{
    Task<IEnumerable<GeneralChatMessage>> GetByUserIdAsync(int userId, int limit = 20);
    Task AddAsync(GeneralChatMessage message);
    Task DeleteAllByUserIdAsync(int userId);
    Task SaveChangesAsync();
}