using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RecipeApp.Application.ChatMessages.DTO;
using RecipeApp.Application.ChatMessages.Interface;
using RecipeApp.Application.Common.Exceptions;
using System.Security.Claims;

namespace RecipeApp.Api.Controllers;

[Authorize]
[Route("api/recipes/{recipeId}/messages")]
[ApiController]
public class ChatMessageController : ControllerBase
{
    private readonly IChatMessageService _chatMessageService;

    public ChatMessageController(IChatMessageService chatMessageService)
    {
        _chatMessageService = chatMessageService;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage(int recipeId, [FromBody] ChatMessageRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new { value = "Token inválido ou sem ID", statusCode = 401 });

        try
        {
            var response = await _chatMessageService.SendMessageAsync(userId.Value, recipeId, request);
            return Ok(response);
        }
        catch (RecipeNotFoundException)
        {
            return NotFound();
        }
        catch (ForbiddenAccessException)
        {
            return NotFound(); // mesmo motivo de sempre: não vaza que o ID existe
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetMessages(int recipeId)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new { value = "Token inválido ou sem ID", statusCode = 401 });

        try
        {
            var messages = await _chatMessageService.GetMessagesByRecipeIdAsync(userId.Value, recipeId);
            return Ok(messages);
        }
        catch (RecipeNotFoundException)
        {
            return NotFound();
        }
        catch (ForbiddenAccessException)
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
