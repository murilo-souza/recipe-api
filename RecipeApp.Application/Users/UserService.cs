using RecipeApp.Application.Users.DTO;
using RecipeApp.Application.Users.Interface;
using RecipeApp.Application.Common.Exceptions;

namespace RecipeApp.Application.Users;

public class UserService : IUserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository) => _repository = repository;

    public async Task<UserResponse?> GetProfileAsync(int userId)
    {
        var user = await _repository.GetByIdWithGoogleLoginAsync(userId);
        if (user is null) return null;

        return new UserResponse(user.Id, user.Name, user.Email, ResolveProfileImage(user));
    }

    public async Task<UserResponse> UpdateProfileAsync(int userId, UpdateUserRequest request)
    {
        var user = await _repository.GetByIdWithGoogleLoginAsync(userId);
        if (user is null) throw new UserNotFoundException();

        user.Name = request.Name;
        user.ProfileImage = request.ProfileImage;

        await _repository.SaveChangesAsync();

        return new UserResponse(user.Id, user.Name, user.Email, ResolveProfileImage(user));
    }

    private static string? ResolveProfileImage(Domain.Entities.User user)
    {
        if (!string.IsNullOrEmpty(user.ProfileImage))
            return user.ProfileImage;

        var googleLogin = user.ExternalLogins.FirstOrDefault(e => e.Provider == "google");
        return googleLogin?.PictureUrl;
    }
}