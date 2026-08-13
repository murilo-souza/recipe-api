namespace RecipeApp.Application.Users.DTO;

public record UpdateUserRequest(string Name, string? ProfileImage);

public record UserResponse(int Id, string Name, string Email, string? ProfileImage);