using RecipeApp.Application.ChatMessages.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace RecipeApp.Application.ChatMessages.Interface;

public interface IChatMessageService
{
    Task<ChatMessageResponse> SendMessageAsync(int userId, int recipeId, ChatMessageRequest request);
    Task<IEnumerable<ChatMessageResponse>> GetMessagesByRecipeIdAsync(int userId, int recipeId);
    Task DeleteAllMessagesByRecipeIdAsync(int userId, int recipeId);
}
