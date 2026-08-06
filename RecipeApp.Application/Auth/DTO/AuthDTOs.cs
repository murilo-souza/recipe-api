namespace RecipeApp.Application.Auth.DTO;

public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record GoogleLoginRequest(string IdToken);
public record AuthResponse(string AccessToken, string UserName, string Email);