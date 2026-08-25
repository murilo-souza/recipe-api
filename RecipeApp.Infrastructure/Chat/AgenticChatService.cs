using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using RecipeApp.Application.Chat.DTO;
using RecipeApp.Application.Chat.Interface;
using RecipeApp.Infrastructure.Common;
using System.Text;
using System.Text.Json;

namespace RecipeApp.Infrastructure.Chat;

public class AgenticChatService : IAgenticChatService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private const string GeminiApiUrl =
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent";

    public AgenticChatService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<string> GenerateReplyAsync(int userId, IEnumerable<AgenticChatTurn> history, string userMessage)
    {
        // 1. Conecta no MCP server e descobre as tools disponíveis
        var mcpServerUrl = _config["Mcp:ServerUrl"]!;
        await using var mcpClient = await McpClient.CreateAsync(
            new HttpClientTransport(new HttpClientTransportOptions { Endpoint = new Uri(mcpServerUrl) }));

        var mcpTools = await mcpClient.ListToolsAsync();

        // 2. Converte as tools MCP pro formato de function declaration do Gemini
        var functionDeclarations = mcpTools.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            parameters = ConvertSchemaForGemini(t.JsonSchema)
        });

        var systemInstruction = new
        {
            parts = new[]
            {
                new { text = $"Você é um assistente de receitas. O userId do usuário atual é {userId} — sempre use esse valor ao chamar tools que pedem userId. Use as ferramentas disponíveis quando a pergunta exigir buscar dados reais das receitas do usuário." }
            }
        };

        var contents = new List<object>();
        foreach (var turn in history)
            contents.Add(new { role = turn.Role, parts = new[] { new { text = turn.Content } } });
        contents.Add(new { role = "user", parts = new[] { new { text = userMessage } } });

        // 3. Primeira chamada ao Gemini, com as tools declaradas
        var firstResponse = await CallGeminiAsync(systemInstruction, contents, functionDeclarations);
        var candidate = firstResponse.RootElement.GetProperty("candidates")[0].GetProperty("content");
        var parts = candidate.GetProperty("parts");

        var functionCallPart = parts.EnumerateArray().FirstOrDefault(p => p.TryGetProperty("functionCall", out _));

        // 4a. Se o modelo respondeu com texto direto, devolve
        if (functionCallPart.ValueKind == JsonValueKind.Undefined)
        {
            return parts[0].GetProperty("text").GetString() ?? "Não consegui gerar uma resposta.";
        }

        // 4b. Se o modelo pediu pra chamar uma tool
        var functionCall = functionCallPart.GetProperty("functionCall");
        var toolName = functionCall.GetProperty("name").GetString()!;
        var argsJson = functionCall.GetProperty("args");

        var arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(argsJson.GetRawText())!;

        var toolResult = await mcpClient.CallToolAsync(toolName, arguments);
        var toolResultText = string.Join("\n", toolResult.Content
         .OfType<TextContentBlock>()
         .Select(c => c.Text));

        // 5. Manda o resultado da tool de volta pro Gemini, pra formular a resposta final
        contents.Add(new { role = "model", parts = new[] { functionCallPart } });
        contents.Add(new
        {
            role = "user",
            parts = new[]
            {
                new
                {
                    functionResponse = new
                    {
                        name = toolName,
                        response = new { result = toolResultText }
                    }
                }
            }
        });

        var finalResponse = await CallGeminiAsync(systemInstruction, contents, functionDeclarations);
        var finalText = finalResponse.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return finalText ?? "Não consegui gerar uma resposta.";
    }

    private async Task<JsonDocument> CallGeminiAsync(object systemInstruction, List<object> contents, IEnumerable<object> functionDeclarations)
    {
        var payload = new
        {
            system_instruction = systemInstruction,
            contents,
            tools = new[] { new { function_declarations = functionDeclarations } }
        };

        var apiKey = _config["Gemini:ApiKey"]!;
        var jsonPayload = JsonSerializer.Serialize(payload);

        var responseString = await GeminiHttpHelper.SendWithRetryAsync(_httpClient, GeminiApiUrl, apiKey, jsonPayload);

        return JsonDocument.Parse(responseString);
    }

    private static object ConvertSchemaForGemini(JsonElement schema)
    {
        var properties = new Dictionary<string, object>();

        if (schema.TryGetProperty("properties", out var propsElement))
        {
            foreach (var prop in propsElement.EnumerateObject())
            {
                var propSchema = new Dictionary<string, object>();

                if (prop.Value.TryGetProperty("type", out var typeEl))
                    propSchema["type"] = ExtractType(typeEl); // usa a função nova

                if (prop.Value.TryGetProperty("description", out var descEl))
                    propSchema["description"] = descEl.GetString()!;

                properties[prop.Name] = propSchema;
            }
        }

        var required = new List<string>();
        if (schema.TryGetProperty("required", out var reqElement))
            required.AddRange(reqElement.EnumerateArray().Select(r => r.GetString()!));

        return new
        {
            type = "OBJECT",
            properties,
            required
        };
    }

    private static string ExtractType(JsonElement typeElement)
    {
        if (typeElement.ValueKind == JsonValueKind.String)
            return typeElement.GetString()!.ToUpperInvariant();

        if (typeElement.ValueKind == JsonValueKind.Array)
        {
            // Pega o primeiro tipo que não seja "null" (ex: ["string", "null"] → "string")
            var firstNonNull = typeElement.EnumerateArray()
                .Select(e => e.GetString())
                .FirstOrDefault(t => t != "null");

            return (firstNonNull ?? "STRING").ToUpperInvariant();
        }

        return "STRING"; // fallback seguro
    }
}