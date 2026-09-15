using System;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeApp.ObservabilityAgent.Services;

public class ObservabilityAgentService
{
    private readonly ILogAnalyticsService _logAnalytics;
    private readonly IGeminiAnalysisService _gemini;
    private readonly IEmailNotificationService _email;

    public ObservabilityAgentService(ILogAnalyticsService logAnalytics, IGeminiAnalysisService gemini, IEmailNotificationService email)
    {
        _logAnalytics = logAnalytics;
        _gemini = gemini;
        _email = email;
    }

    public async Task<string> RunAnalysisAsync(TimeSpan lookback)
    {
        var logs = await _logAnalytics.QueryRecentLogsAsync(lookback);

        if (!logs.Any())
        {
            var noLogsMessage = "Nenhum log encontrado no período analisado.";
            await _email.SendAsync("Relatório de Observabilidade — RecipeApp", noLogsMessage);
            return noLogsMessage;
        }

        var analysis = await _gemini.AnalyzeAsync(logs);
        await _email.SendAsync("Relatório de Observabilidade — RecipeApp", analysis);
        return analysis;
    }
}