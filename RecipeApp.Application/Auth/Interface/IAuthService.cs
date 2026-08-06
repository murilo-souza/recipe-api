using RecipeApp.Application.Auth.DTO;

namespace RecipeApp.Application.Auth.Interface;

public interface IAuthService
{
    Task<(bool Success, string? Error, AuthResult? Result)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, string? Error, AuthResult? Result)> LoginAsync(LoginRequest request);
    Task<(bool Success, string? Error, AuthResult? Result)> GoogleLoginAsync(GoogleLoginRequest request);
    Task<(bool Success, AuthResult? Result)> RefreshAsync(string rawRefreshToken);
    Task LogoutAsync(string rawRefreshToken);
}

// Carrega tudo que o Controller precisa pra montar a resposta HTTP (corpo + cookie)
public record AuthResult(string AccessToken, string RefreshTokenRaw, DateTimeOffset RefreshTokenExpiresAt, string UserName, string Email);