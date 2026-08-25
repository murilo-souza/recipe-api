using System.Net;
using System.Text;

namespace RecipeApp.Infrastructure.Common;

public static class GeminiHttpHelper
{
    public static async Task<string> SendWithRetryAsync(
        HttpClient httpClient,
        string url,
        string apiKey,
        string jsonPayload,
        int maxRetries = 2)
    {
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", apiKey);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                return responseString;

            var isRetryable = response.StatusCode == HttpStatusCode.ServiceUnavailable
                || response.StatusCode == HttpStatusCode.TooManyRequests;

            if (!isRetryable || attempt == maxRetries)
                throw new Exception($"Gemini API error ({response.StatusCode}): {responseString}");

            var delaySeconds = Math.Pow(2, attempt);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }

        throw new Exception("Falha ao chamar a API do Gemini após múltiplas tentativas.");
    }
}