using Microsoft.EntityFrameworkCore;
using RecipeApp.Application.Chat.Interface;
using RecipeApp.Domain.Entities;
using RecipeApp.Infrastructure.Persistence;

namespace RecipeApp.Infrastructure.Chat;

public class GeneralChatRepository : IGeneralChatRepository
{
    private readonly AppDbContext _db;
    public GeneralChatRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<GeneralChatMessage>> GetByUserIdAsync(int userId, int limit = 20)
        => await _db.GeneralChatMessages
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(GeneralChatMessage message)
        => await _db.GeneralChatMessages.AddAsync(message);

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}