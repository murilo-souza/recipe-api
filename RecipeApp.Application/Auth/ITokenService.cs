using RecipeApp.Domain.Entities;

namespace RecipeApp.Application.Auth;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken(); // token opaco, aleatório
    string HashToken(string token);
}