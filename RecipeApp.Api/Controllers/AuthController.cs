using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeApp.Application.Auth;
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

    public AuthController(AppDbContext db, ITokenService tokenService, IConfiguration config)
    {
        _db = db;
        _tokenService = tokenService;
        _config = config;
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

        // Rotação: revoga o antigo, emite um novo
        stored.RevokedAt = DateTimeOffset.UtcNow;
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
                stored.RevokedAt = DateTimeOffset.UtcNow;
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
            Path = "/api/auth" // só é enviado pras rotas de auth, não em toda requisição
        });

        return new AuthResponse(accessToken, user.Name, user.Email);
    }
}