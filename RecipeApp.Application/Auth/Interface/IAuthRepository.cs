using RecipeApp.Domain.Entities;

namespace RecipeApp.Application.Auth.Interface;

public interface IAuthRepository
{
    Task<User?> GetUserByEmailAsync(string email);
    Task AddUserAsync(User user);
    Task<ExternalLogin?> GetExternalLoginAsync(string provider, string providerUserId);
    Task AddExternalLoginAsync(ExternalLogin externalLogin);
    Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash);
    Task AddRefreshTokenAsync(RefreshToken refreshToken);
    Task RemoveRefreshTokenAsync(RefreshToken refreshToken);
    Task AddPasswordResetCodeAsync(PasswordResetCode code);
    Task<PasswordResetCode?> GetLatestValidResetCodeAsync(int userId, string codeHash);
    Task<User?> GetUserByIdAsync(int userId);

    Task SaveChangesAsync();
}