using Microsoft.Extensions.Configuration;
using RecipeApp.Application.Gemini.Interface;
using RecipeApp.Domain.Entities;
using RecipeApp.Infrastructure.Common;
using System.Text;
using System.Text.Json;

namespace RecipeApp.Infrastructure.Gemini
{
    public class GeminiService: IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string GeminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent";

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"]
                  ?? throw new ArgumentNullException("Gemini:ApiKey", "A chave da API do Gemini não foi encontrada.");
        }
        public async Task<string> GenerateReplyAsync(Recipe recipe, IEnumerable<ChatMessage> chatHistory)
        {
            
            // 1. Monta o contexto da receita para o Gemini agir como especialista nela
            var systemInstruction = new
            {
                parts = new[]
                {
                new { text = $@"Você é um assistente culinário amigável. 
                        O usuário está visualizando a seguinte receita e vai tirar dúvidas sobre ela.
                        Responda sempre com base nesta receita. Se ele perguntar algo fora de culinária, seja educado e recuse.
                        RECEITA: {recipe.Title}
                        DESCRIÇÃO: {recipe.Description}

                        INGREDIENTES: 
                        {string.Join("\n- ", recipe.Ingredients.OrderBy(i => i.Position).Select(i => i.Description))} 

                        INSTRUÇÕES: 
                        {string.Join("\n- ", recipe.PrepareSteps.OrderBy(p => p.Position).Select(p => p.Description))}" 
                }
            }
            };

            // 2. Mapeia o seu histórico do banco para o formato do Gemini
            // O Gemini aceita os roles: "user" e "model"
            var contents = chatHistory.Select(msg => new
            {
                role = msg.Role == ChatRole.User ? "user" : "model",
                parts = new[] { new { text = msg.Content } }
            }).ToList();

            var payload = new
            {
                system_instruction = systemInstruction,
                contents = contents
            };

            var jsonPayload = JsonSerializer.Serialize(payload);

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, GeminiApiUrl);
            requestMessage.Headers.Add("x-goog-api-key", _apiKey);
            requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var responseString = await GeminiHttpHelper.SendWithRetryAsync(_httpClient, GeminiApiUrl, _apiKey, jsonPayload);

            using var document = JsonDocument.Parse(responseString);

            return document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "Desculpe, não consegui gerar uma resposta.";
        }


    }
}
