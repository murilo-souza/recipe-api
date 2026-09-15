using System.Threading.Tasks;

namespace RecipeApp.ObservabilityAgent.Services;

public interface IEmailNotificationService
{
    Task SendAsync(string subject, string body);
}