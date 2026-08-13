// RecipeApp.Application/Users/Interface/IUserRepository.cs
using RecipeApp.Domain.Entities;

namespace RecipeApp.Application.Users.Interface;

public interface IUserRepository
{
    Task<User?> GetByIdWithGoogleLoginAsync(int userId);
    Task SaveChangesAsync();
}