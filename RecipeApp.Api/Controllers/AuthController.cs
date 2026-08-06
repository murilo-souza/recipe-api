using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeApp.Application.Auth;
using RecipeApp.Application.Auth.DTO;
using RecipeApp.Application.Auth.Interface;
using RecipeApp.Domain.Entities;
using RecipeApp.Infrastructure.Persistence;

namespace RecipeApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;
    private readonly IGoogleAuthValidator _googleValidator;

    public AuthController(AppDbContext db, ITokenService tokenService, IConfiguration config, IGoogleAuthValidator googleValidator)
    {
        _db = db;
        _tokenService = tokenService;
        _config = config;
        _googleValidator = googleValidator;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return Conflict("Email já cadastrado.");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return await IssueTokens(user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == request.Email);

        if (user is null || user.PasswordHash is null ||
            !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Email ou senha inválidos.");

        return await IssueTokens(user);
    }

    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin(GoogleLoginRequest request)
    {
        var googleUser = await _googleValidator.ValidateAsync(request.IdToken);
        if (googleUser is null)
            return Unauthorized("Token do Google inválido.");

        // 1. Já existe um login externo vinculado a esse Google Sub?
        var existingLogin = await _db.ExternalLogins
            .Include(e => e.User)
            .SingleOrDefaultAsync(e => e.Provider == "google" && e.ProviderUserId == googleUser.Sub);

        if (existingLogin is not null)
            return await IssueTokens(existingLogin.User);

        // 2. Não tem login externo ainda — já existe um User com esse email
        //    (ex: cadastrou por email/senha antes, agora está logando via Google)?
        var existingUser = await _db.Users.SingleOrDefaultAsync(u => u.Email == googleUser.Email);

        if (existingUser is not null)
        {
            _db.ExternalLogins.Add(new ExternalLogin
            {
                UserId = existingUser.Id,
                Provider = "google",
                ProviderUserId = googleUser.Sub
            });
            await _db.SaveChangesAsync();

            return await IssueTokens(existingUser);
        }

        // 3. Usuário novo — cria User (sem senha) + ExternalLogin
        var newUser = new User
        {
            Name = googleUser.Name,
            Email = googleUser.Email,
            PasswordHash = null // login só via Google
        };

        _db.Users.Add(newUser);
        await _db.SaveChangesAsync(); // precisa salvar antes pra ter o Id gerado

        _db.ExternalLogins.Add(new ExternalLogin
        {
            UserId = newUser.Id,
            Provider = "google",
            ProviderUserId = googleUser.Sub
        });
        await _db.SaveChangesAsync();

        return await IssueTokens(newUser);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh()
    {
        var rawToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(rawToken))
            return Unauthorized();

        var hash = _tokenService.HashToken(rawToken);
        var stored = await _db.RefreshTokens
            .Include(r => r.User)
            .SingleOrDefaultAsync(r => r.TokenHash == hash);

        if (stored is null || !stored.IsActive)
            return Unauthorized();

        // Rotação: apaga o token usado (em vez de só marcar como revogado)
        // evita acúmulo de linhas mortas na tabela a cada renovação
        _db.RefreshTokens.Remove(stored);
        await _db.SaveChangesAsync();

        return await IssueTokens(stored.User);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var rawToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(rawToken))
        {
            var hash = _tokenService.HashToken(rawToken);
            var stored = await _db.RefreshTokens.SingleOrDefaultAsync(r => r.TokenHash == hash);
            if (stored is not null)
            {
                _db.RefreshTokens.Remove(stored); // idem: delete em vez de revoke
                await _db.SaveChangesAsync();
            }
        }

        Response.Cookies.Delete("refreshToken");
        return NoContent();
    }

    private async Task<AuthResponse> IssueTokens(User user)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenRaw = _tokenService.GenerateRefreshToken();
        var refreshDays = double.Parse(_config["Jwt:RefreshTokenDays"]!);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashToken(refreshTokenRaw),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(refreshDays)
        });
        await _db.SaveChangesAsync();

        Response.Cookies.Append("refreshToken", refreshTokenRaw, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(refreshDays),
            Path = "/api/auth"
        });

        return new AuthResponse(accessToken, user.Name, user.Email);
    }
}