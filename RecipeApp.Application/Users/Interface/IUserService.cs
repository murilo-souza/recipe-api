// RecipeApp.Application/Users/Interface/IUserService.cs
using RecipeApp.Application.Users.DTO;

namespace RecipeApp.Application.Users.Interface;

public interface IUserService
{
    Task<UserResponse?> GetProfileAsync(int userId);
    Task<UserResponse> UpdateProfileAsync(int userId, UpdateUserRequest request);
}