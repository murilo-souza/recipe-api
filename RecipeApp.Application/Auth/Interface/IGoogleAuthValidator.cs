namespace RecipeApp.Application.Auth;

public record GoogleUserInfo(string Sub, string Email, string Name, string? Picture);

public interface IGoogleAuthValidator
{
    Task<GoogleUserInfo?> ValidateAsync(string idToken);
}