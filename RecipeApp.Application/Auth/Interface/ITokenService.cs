using RecipeApp.Domain.Entities;

namespace RecipeApp.Application.Auth.Interface;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken(); // token opaco, aleatório
    string HashToken(string token);
    string GeneratePasswordResetToken(int userId);
    int? ValidatePasswordResetToken(string token);
}