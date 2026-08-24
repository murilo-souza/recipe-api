using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecipeApp.Application.Chat.DTO;
using RecipeApp.Application.Chat.Interface;
using System.Security.Claims;

namespace RecipeApp.Api.Controllers;

[Authorize]
[Route("api/chat/general")]
[ApiController]
public class GeneralChatController : ControllerBase
{
    private readonly IGeneralChatService _service;
    public GeneralChatController(IGeneralChatService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] GeneralChatMessageRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var response = await _service.SendMessageAsync(userId.Value, request);
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetHistory()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var history = await _service.GetHistoryAsync(userId.Value);
        return Ok(history);
    }

    private int? GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdStr, out int userId) ? userId : null;
    }
}