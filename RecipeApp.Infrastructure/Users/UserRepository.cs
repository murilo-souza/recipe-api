using Microsoft.EntityFrameworkCore;
using RecipeApp.Application.Users.Interface;
using RecipeApp.Domain.Entities;
using RecipeApp.Infrastructure.Persistence;

namespace RecipeApp.Infrastructure.Users;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public async Task<User?> GetByIdWithGoogleLoginAsync(int userId)
        => await _db.Users
            .Include(u => u.ExternalLogins)
            .SingleOrDefaultAsync(u => u.Id == userId);

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}