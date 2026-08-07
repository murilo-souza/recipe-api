using System;
using System.Collections.Generic;
using System.Text;

namespace RecipeApp.Application.ChatMessages.DTO;

public record ChatMessageRequest(string Content, int RecipeId);

public record ChatMessageResponse(int Id, string Role, string Content, int RecipeId, DateTimeOffset CreatedAt);