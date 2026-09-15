using System;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeApp.ObservabilityAgent.Services;

public class ObservabilityAgentService
{
    private readonly ILogAnalyticsService _logAnalytics;
    private readonly IGeminiAnalysisService _gemini;

    public ObservabilityAgentService(ILogAnalyticsService logAnalytics, IGeminiAnalysisService gemini)
    {
        _logAnalytics = logAnalytics;
        _gemini = gemini;
    }

    public async Task<string> RunAnalysisAsync(TimeSpan lookback)
    {
        var logs = await _logAnalytics.QueryRecentLogsAsync(lookback);

        if (!logs.Any())
            return "Nenhum log encontrado no período analisado.";

        return await _gemini.AnalyzeAsync(logs);
    }
}