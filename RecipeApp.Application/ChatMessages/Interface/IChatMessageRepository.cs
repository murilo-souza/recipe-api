using RecipeApp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RecipeApp.Application.ChatMessages.Interface;

public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage chatMessage);
}
