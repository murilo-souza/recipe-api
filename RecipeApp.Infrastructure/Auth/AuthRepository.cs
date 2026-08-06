using Microsoft.EntityFrameworkCore;
using RecipeApp.Application.Auth.Interface;
using RecipeApp.Domain.Entities;
using RecipeApp.Infrastructure.Persistence;

namespace RecipeApp.Infrastructure.Auth;

public class AuthRepository : IAuthRepository
{
    private readonly AppDbContext _db;

    public AuthRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
        => await _db.Users.SingleOrDefaultAsync(u => u.Email == email);

    public async Task AddUserAsync(User user)
        => await _db.Users.AddAsync(user);

    public async Task<ExternalLogin?> GetExternalLoginAsync(string provider, string providerUserId)
        => await _db.ExternalLogins
            .Include(e => e.User)
            .SingleOrDefaultAsync(e => e.Provider == provider && e.ProviderUserId == providerUserId);

    public async Task AddExternalLoginAsync(ExternalLogin externalLogin)
        => await _db.ExternalLogins.AddAsync(externalLogin);

    public async Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash)
        => await _db.RefreshTokens
            .Include(r => r.User)
            .SingleOrDefaultAsync(r => r.TokenHash == tokenHash);

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
        => await _db.RefreshTokens.AddAsync(refreshToken);

    public async Task RemoveRefreshTokenAsync(RefreshToken refreshToken)
    {
        _db.RefreshTokens.Remove(refreshToken);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
        => await _db.SaveChangesAsync();
}