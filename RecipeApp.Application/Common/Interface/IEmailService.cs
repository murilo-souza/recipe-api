// RecipeApp.Application/Common/Interface/IEmailService.cs
namespace RecipeApp.Application.Common.Interface;

public interface IEmailService
{
    Task SendPasswordResetCodeAsync(string toEmail, string userName, string code);
}