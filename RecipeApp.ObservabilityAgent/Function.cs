using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace RecipeApp.ObservabilityAgent;

public class Function
{
    private readonly ILogger _logger;

    public Function(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Function>();
    }

    [Function("Function")]
    public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);
        
        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }
    }
}