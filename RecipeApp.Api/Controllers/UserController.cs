using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecipeApp.Application.Users.DTO;
using RecipeApp.Application.Users.Interface;
using System.Security.Claims;

namespace RecipeApp.Api.Controllers;

[Authorize]
[Route("api/user")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var profile = await _userService.GetProfileAsync(userId.Value);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var profile = await _userService.UpdateProfileAsync(userId.Value, request);
            return Ok(profile);
        }
        catch (UserNotFoundException)
        {
            return NotFound();
        }
    }

    private int? GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdStr, out int userId) ? userId : null;
    }
}