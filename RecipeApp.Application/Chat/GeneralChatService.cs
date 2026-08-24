using RecipeApp.Application.Chat.DTO;
using RecipeApp.Application.Chat.Interface;
using RecipeApp.Domain.Entities;

namespace RecipeApp.Application.Chat;

public class GeneralChatService : IGeneralChatService
{
    private readonly IGeneralChatRepository _repository;
    private readonly IAgenticChatService _agenticChatService;

    public GeneralChatService(IGeneralChatRepository repository, IAgenticChatService agenticChatService)
    {
        _repository = repository;
        _agenticChatService = agenticChatService;
    }

    public async Task<GeneralChatMessageResponse> SendMessageAsync(int userId, GeneralChatMessageRequest request)
    {
        var userMessage = new GeneralChatMessage
        {
            UserId = userId,
            Role = ChatRole.User,
            Content = request.Content,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _repository.AddAsync(userMessage);
        await _repository.SaveChangesAsync();

        var history = await _repository.GetByUserIdAsync(userId);
        var turns = history
            .Where(m => m.Id != userMessage.Id)
            .Select(m => new AgenticChatTurn(m.Role == ChatRole.User ? "user" : "model", m.Content));

        string aiText;
        try
        {
            aiText = await _agenticChatService.GenerateReplyAsync(userId, turns, request.Content);
        }
        catch
        {
            aiText = "Desculpe, não consegui processar sua pergunta agora. Tente novamente em instantes.";
        }

        var aiMessage = new GeneralChatMessage
        {
            UserId = userId,
            Role = ChatRole.Assistant,
            Content = aiText,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _repository.AddAsync(aiMessage);
        await _repository.SaveChangesAsync();

        return new GeneralChatMessageResponse(aiMessage.Id, aiMessage.Role.ToString(), aiMessage.Content, aiMessage.CreatedAt);
    }

    public async Task<IEnumerable<GeneralChatMessageResponse>> GetHistoryAsync(int userId)
    {
        var messages = await _repository.GetByUserIdAsync(userId);
        return messages.Select(m => new GeneralChatMessageResponse(m.Id, m.Role.ToString(), m.Content, m.CreatedAt));
    }
}