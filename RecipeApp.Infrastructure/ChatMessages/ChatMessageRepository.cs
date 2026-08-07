using Microsoft.EntityFrameworkCore;
using RecipeApp.Application.ChatMessages.Interface;
using RecipeApp.Domain.Entities;
using RecipeApp.Infrastructure.Persistence;


namespace RecipeApp.Infrastructure.ChatMessages
{
    public class ChatMessageRepository: IChatMessageRepository
    {
        private readonly AppDbContext _db;
        public ChatMessageRepository(AppDbContext db)
        {
            _db = db;
        }


        public async Task AddAsync(ChatMessage chatMessage)
        {
            await _db.ChatMessages.AddAsync(chatMessage);
        }

        public async Task<IEnumerable<ChatMessage>> GetByRecipeIdAsync(int recipeId)
        {
            return await _db.ChatMessages
                .Where(cm => cm.RecipeId == recipeId)
                .OrderBy(cm => cm.CreatedAt)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
