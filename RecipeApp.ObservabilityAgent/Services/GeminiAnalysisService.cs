using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RecipeApp.ObservabilityAgent.Services;

public class GeminiAnalysisService : IGeminiAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GeminiAnalysisService> _logger;

    private const string SystemPrompt = """
        Você é um engenheiro de confiabilidade analisando logs de uma aplicação .NET
        hospedada em Azure Container Apps. Recebe uma lista de logs recentes.

        Sua tarefa:
        1. Identificar se há erros reais ou apenas ruído operacional.
        2. Correlacionar eventos próximos no tempo que possam ter causa comum.
        3. Apontar a causa raiz mais provável, quando houver evidência suficiente.
        4. Sugerir o próximo passo de investigação.

        Não invente causas sem evidência nos logs. Se estiver tudo normal, diga isso
        em uma frase. Responda em português, de forma objetiva, em no máximo 200 palavras.
        """;

    public GeminiAnalysisService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiAnalysisService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey não configurado.");
        _logger = logger;
    }

    public async Task<string> AnalyzeAsync(IReadOnlyList<LogEntry> logs)
    {
        var formattedLogs = string.Join("\n", logs.Select(l =>
            $"[{l.Timestamp:yyyy-MM-dd HH:mm:ss}] [{l.ContainerApp}] {l.Message}"));

        var payload = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = SystemPrompt } }
            },
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = $"Logs para análise:\n\n{formattedLogs}" } }
                }
            }
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent?key={_apiKey}";

        var response = await _httpClient.PostAsJsonAsync(url, payload);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        _logger.LogInformation("Análise concluída pelo Gemini.");
        return text ?? "Não foi possível obter uma análise.";
    }
}