namespace RecipeApp.Application.Auth;

public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string AccessToken, string UserName, string Email);