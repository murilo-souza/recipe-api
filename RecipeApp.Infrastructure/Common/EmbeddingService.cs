using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RecipeApp.Application.Common.Interface;

namespace RecipeApp.Infrastructure.Common;

public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string EmbeddingApiUrl =
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent";

    public EmbeddingService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"]
            ?? throw new ArgumentNullException("Gemini:ApiKey", "A chave da API do Gemini não foi encontrada.");
    }

    public async Task<float[]?> GenerateEmbeddingAsync(string text)
    {
        var payload = new
        {
            content = new { parts = new[] { new { text } } },
            outputDimensionality = 768,
            taskType = "RETRIEVAL_DOCUMENT"
        };

        var jsonPayload = JsonSerializer.Serialize(payload);

        try
        {
            var responseString = await GeminiHttpHelper.SendWithRetryAsync(_httpClient, EmbeddingApiUrl, _apiKey, jsonPayload);
            using var document = JsonDocument.Parse(responseString);

            var values = document.RootElement
                .GetProperty("embedding")
                .GetProperty("values")
                .EnumerateArray()
                .Select(v => v.GetSingle())
                .ToArray();

            return values;
        } catch
        {
            return null;
        }
    }
}