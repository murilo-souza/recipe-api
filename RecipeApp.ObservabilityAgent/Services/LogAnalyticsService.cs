using Azure.Identity;
using Azure.Monitor.Query.Logs;
using Azure.Monitor.Query.Logs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecipeApp.ObservabilityAgent.Services;

public class LogAnalyticsService : ILogAnalyticsService
{
    private readonly LogsQueryClient _client;
    private readonly string _workspaceId;
    private readonly ILogger<LogAnalyticsService> _logger;

    public LogAnalyticsService(IConfiguration configuration, ILogger<LogAnalyticsService> logger)
    {
        _workspaceId = configuration["LogAnalytics:WorkspaceId"]
            ?? throw new InvalidOperationException("LogAnalytics:WorkspaceId não configurado.");
        _client = new LogsQueryClient(new DefaultAzureCredential());
        _logger = logger;
    }

    public async Task<IReadOnlyList<LogEntry>> QueryRecentLogsAsync(TimeSpan lookback)
    {
        var query = """
            ContainerAppConsoleLogs_CL
            | where ContainerAppName_s in ("recipe-api", "recipe-mcpserver")
            | project TimeGenerated, ContainerAppName_s, Log_s
            | order by TimeGenerated desc
            | take 200
            """;

        var response = await _client.QueryWorkspaceAsync(
            _workspaceId,
            query,
            new LogsQueryTimeRange(lookback));

        var entries = new List<LogEntry>();

        foreach (var row in response.Value.Table.Rows)
        {
            entries.Add(new LogEntry(
                row.GetDateTimeOffset("TimeGenerated") ?? DateTimeOffset.MinValue,
                row.GetString("ContainerAppName_s") ?? "desconhecido",
                row.GetString("Log_s") ?? string.Empty));
        }

        _logger.LogInformation("Recuperados {Count} registros de log.", entries.Count);
        return entries;
    }
}