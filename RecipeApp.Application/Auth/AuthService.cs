using RecipeApp.Application.Auth.DTO;
using RecipeApp.Application.Auth.Interface;
using RecipeApp.Domain.Entities;

namespace RecipeApp.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _repository;
    private readonly ITokenService _tokenService;
    private readonly IGoogleAuthValidator _googleValidator;
    private readonly double _refreshTokenDays;

    public AuthService(
        IAuthRepository repository,
        ITokenService tokenService,
        IGoogleAuthValidator googleValidator,
        double refreshTokenDays)
    {
        _repository = repository;
        _tokenService = tokenService;
        _googleValidator = googleValidator;
        _refreshTokenDays = refreshTokenDays;
    }

    public async Task<(bool Success, string? Error, AuthResult? Result)> RegisterAsync(RegisterRequest request)
    {
        if (await _repository.GetUserByEmailAsync(request.Email) is not null)
            return (false, "Email já cadastrado.", null);

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _repository.AddUserAsync(user);
        await _repository.SaveChangesAsync();

        return (true, null, await IssueTokensAsync(user));
    }

    public async Task<(bool Success, string? Error, AuthResult? Result)> LoginAsync(LoginRequest request)
    {
        var user = await _repository.GetUserByEmailAsync(request.Email);

        if (user is null || user.PasswordHash is null ||
            !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return (false, "Email ou senha inválidos.", null);

        return (true, null, await IssueTokensAsync(user));
    }

    public async Task<(bool Success, string? Error, AuthResult? Result)> GoogleLoginAsync(GoogleLoginRequest request)
    {
        var googleUser = await _googleValidator.ValidateAsync(request.IdToken);
        if (googleUser is null)
            return (false, "Token do Google inválido.", null);

        var existingLogin = await _repository.GetExternalLoginAsync("google", googleUser.Sub);
        if (existingLogin is not null)
            return (true, null, await IssueTokensAsync(existingLogin.User));

        var existingUser = await _repository.GetUserByEmailAsync(googleUser.Email);
        if (existingUser is not null)
        {
            await _repository.AddExternalLoginAsync(new ExternalLogin
            {
                UserId = existingUser.Id,
                Provider = "google",
                ProviderUserId = googleUser.Sub,
                PictureUrl = googleUser.Picture

            });
            await _repository.SaveChangesAsync();

            return (true, null, await IssueTokensAsync(existingUser));
        }

        var newUser = new User
        {
            Name = googleUser.Name,
            Email = googleUser.Email,
            PasswordHash = null
        };

        await _repository.AddUserAsync(newUser);
        await _repository.SaveChangesAsync(); // precisa gerar o Id antes do ExternalLogin

        await _repository.AddExternalLoginAsync(new ExternalLogin
        {
            UserId = newUser.Id,
            Provider = "google",
            ProviderUserId = googleUser.Sub
        });
        await _repository.SaveChangesAsync();

        return (true, null, await IssueTokensAsync(newUser));
    }

    public async Task<(bool Success, AuthResult? Result)> RefreshAsync(string rawRefreshToken)
    {
        var hash = _tokenService.HashToken(rawRefreshToken);
        var stored = await _repository.GetRefreshTokenByHashAsync(hash);

        if (stored is null || !stored.IsActive)
            return (false, null);

        await _repository.RemoveRefreshTokenAsync(stored);
        await _repository.SaveChangesAsync();

        return (true, await IssueTokensAsync(stored.User));
    }

    public async Task LogoutAsync(string rawRefreshToken)
    {
        var hash = _tokenService.HashToken(rawRefreshToken);
        var stored = await _repository.GetRefreshTokenByHashAsync(hash);

        if (stored is not null)
        {
            await _repository.RemoveRefreshTokenAsync(stored);
            await _repository.SaveChangesAsync();
        }
    }

    private async Task<AuthResult> IssueTokensAsync(User user)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenRaw = _tokenService.GenerateRefreshToken();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_refreshTokenDays);

        await _repository.AddRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashToken(refreshTokenRaw),
            ExpiresAt = expiresAt
        });
        await _repository.SaveChangesAsync();

        return new AuthResult(accessToken, refreshTokenRaw, expiresAt, user.Name, user.Email);
    }
}