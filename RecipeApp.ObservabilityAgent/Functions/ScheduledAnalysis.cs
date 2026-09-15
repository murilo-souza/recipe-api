using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RecipeApp.ObservabilityAgent.Services;
using System;
using System.Threading.Tasks;

namespace RecipeApp.ObservabilityAgent.Functions;

public class ScheduledAnalysis
{
    private readonly ObservabilityAgentService _agent;
    public ScheduledAnalysis(ObservabilityAgentService agent) => _agent = agent;

    [Function("ScheduledAnalysis")]
    public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo timer)
    {
        var result = await _agent.RunAnalysisAsync(TimeSpan.FromMinutes(30));
        // TODO: publicar no Teams/Slack
    }
}
