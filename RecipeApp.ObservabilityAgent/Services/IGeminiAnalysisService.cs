using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecipeApp.ObservabilityAgent.Services;

public interface IGeminiAnalysisService
{
    Task<string> AnalyzeAsync(IReadOnlyList<LogEntry> logs);
}