using RecipeApp.Application.ChatMessages.DTO;
using RecipeApp.Application.ChatMessages.Interface;
using RecipeApp.Application.Common.Exceptions;
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
        public ChatMessageService(IChatMessageRepository chatMessageRepository, IRecipeRepository recipeRepository)
        {
            _chatMessageRepository = chatMessageRepository;
            _recipeRepository = recipeRepository;
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

            return new ChatMessageResponse(message.Id, message.Role.ToString(), message.Content, message.RecipeId, message.CreatedAt);
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
