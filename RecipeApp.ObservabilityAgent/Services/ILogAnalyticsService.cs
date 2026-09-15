using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecipeApp.ObservabilityAgent.Services;

public interface ILogAnalyticsService
{
    Task<IReadOnlyList<LogEntry>> QueryRecentLogsAsync(TimeSpan lookback);
}

public record LogEntry(DateTimeOffset Timestamp, string ContainerApp, string Message);