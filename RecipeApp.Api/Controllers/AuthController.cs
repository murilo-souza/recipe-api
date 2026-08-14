using Microsoft.AspNetCore.Mvc;
using RecipeApp.Application.Auth;
using RecipeApp.Application.Auth.DTO;
using RecipeApp.Application.Auth.Interface;

namespace RecipeApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var (success, error, result) = await _authService.RegisterAsync(request);
        if (!success) return Conflict(error);

        return Ok(BuildResponse(result!));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var (success, error, result) = await _authService.LoginAsync(request);
        if (!success) return Unauthorized(error);

        

        return Ok(BuildResponse(result!));
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request)
    {
        var (success, error, result) = await _authService.GoogleLoginAsync(request);
        if (!success) return Unauthorized(error);

        return Ok(BuildResponse(result!));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var rawToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(rawToken)) return Unauthorized();

        var (success, result) = await _authService.RefreshAsync(rawToken);
        if (!success) return Unauthorized();

        return Ok(BuildResponse(result!));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var rawToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(rawToken))
            await _authService.LogoutAsync(rawToken);

        Response.Cookies.Delete("refreshToken");
        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request);
        return Ok(); // sempre 200, independente do resultado real
    }

    [HttpPost("verify-reset-code")]
    public async Task<IActionResult> VerifyResetCode(VerifyResetCodeRequest request)
    {
        var (success, resetToken) = await _authService.VerifyResetCodeAsync(request);
        if (!success) return BadRequest(new { error = "Código inválido ou expirado." });

        return Ok(new VerifyResetCodeResponse(resetToken!));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var success = await _authService.ResetPasswordAsync(request);
        if (!success) return BadRequest(new { error = "Não foi possível redefinir a senha." });

        return NoContent();
    }

    private AuthResponse BuildResponse(AuthResult result)
    {
        Response.Cookies.Append("refreshToken", result.RefreshTokenRaw, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = result.RefreshTokenExpiresAt,
            Path = "/api/auth"
        });

        return new AuthResponse(result.AccessToken, result.UserName, result.Email);
    }
}