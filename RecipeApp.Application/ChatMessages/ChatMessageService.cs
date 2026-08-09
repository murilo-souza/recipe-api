using RecipeApp.Application.ChatMessages.DTO;
using RecipeApp.Application.ChatMessages.Interface;
using RecipeApp.Application.Common.Exceptions;
using RecipeApp.Application.Gemini.Interface;
using RecipeApp.Application.Recipes.Interface;
using RecipeApp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RecipeApp.Application.ChatMessages
{
    public class ChatMessageService: IChatMessageService
    {
        private readonly IChatMessageRepository _chatMessageRepository;
        private readonly IRecipeRepository _recipeRepository;
        private readonly IGeminiService _geminiService;
        public ChatMessageService(IChatMessageRepository chatMessageRepository, IRecipeRepository recipeRepository, IGeminiService geminiService)
        {
            _chatMessageRepository = chatMessageRepository;
            _recipeRepository = recipeRepository;
            _geminiService = geminiService;
        }
        public async Task<ChatMessageResponse> SendMessageAsync(int userId, int recipeId, ChatMessageRequest request)
        {
            var recipe = await _recipeRepository.GetByIdAsync(recipeId);
            if (recipe is null)
                throw new RecipeNotFoundException();

            if (recipe.UserId != userId)
                throw new ForbiddenAccessException();

            var message = new ChatMessage
            {
                RecipeId = recipeId,
                Role = ChatRole.User,
                Content = request.Content
            };

            await _chatMessageRepository.AddAsync(message);
            await _chatMessageRepository.SaveChangesAsync();

            var history = await _chatMessageRepository.GetRecentByRecipeIdAsync(recipeId);
            string aiResponseText;

            try
            {
                aiResponseText = await _geminiService.GenerateReplyAsync(recipe, history);

            } catch (Exception)
            {
                aiResponseText = "Desculpe, não consegui processar sua pergunta agora. Tente novamente em instantes."; 
            }
            

            var aiMessage = new ChatMessage
            {
                RecipeId = recipeId,
                Role = ChatRole.Assistant,
                Content = aiResponseText
            };

            await _chatMessageRepository.AddAsync(aiMessage);
            await _chatMessageRepository.SaveChangesAsync();

            // Passo 5: Retorna o DTO com a resposta da IA para ser exibida no front-end
            return new ChatMessageResponse(
                aiMessage.Id,
                aiMessage.Role.ToString(),
                aiMessage.Content,
                aiMessage.RecipeId,
                aiMessage.CreatedAt);

        }

        public async Task<IEnumerable<ChatMessageResponse>> GetMessagesByRecipeIdAsync(int userId, int recipeId)
        {
            var recipe = await _recipeRepository.GetByIdAsync(recipeId);

            if (recipe is null)
                throw new RecipeNotFoundException();

            if (recipe.UserId != userId)
                throw new ForbiddenAccessException();

            var messages = await _chatMessageRepository.GetByRecipeIdAsync(recipeId);

            return messages.Select(m => new ChatMessageResponse(m.Id, m.Role.ToString(), m.Content, m.RecipeId, m.CreatedAt));

        }
    }
}
