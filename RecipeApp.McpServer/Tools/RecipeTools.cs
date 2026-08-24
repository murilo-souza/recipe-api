// RecipeApp.McpServer/Tools/RecipeTools.cs
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using RecipeApp.Infrastructure.Persistence;

namespace RecipeApp.McpServer.Tools;

[McpServerToolType]
public class RecipeTools
{
    private readonly AppDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public RecipeTools(AppDbContext db, HttpClient httpClient, IConfiguration config)
    {
        _db = db;
        _httpClient = httpClient;
        _config = config;
    }

    [McpServerTool, Description("Busca receitas por similaridade semântica com a pergunta do usuário. Use para perguntas fuzzy, tipo 'algo leve e rápido' ou 'receitas com sabor picante'.")]
    public async Task<string> SearchRecipesSemantic(
        [Description("O userId do dono das receitas")] int userId,
        [Description("A pergunta ou descrição do que o usuário procura")] string query,
        [Description("Quantidade máxima de resultados")] int limit = 5)
    {
        var queryEmbedding = await GenerateEmbeddingAsync(query);
        if (queryEmbedding is null)
            return "Não foi possível processar a busca no momento.";

        const double maxDistance = 0.322;

        var recipes = await _db.Recipes
        .Where(r => r.UserId == userId && r.Embedding != null)
        .Select(r => new
        {
            r.Id,
            r.Title,
            r.Description,
            r.Category,
            Distance = r.Embedding!.CosineDistance(queryEmbedding)
        })
        .Where(r => r.Distance < maxDistance)
        .OrderBy(r => r.Distance)
        .Take(limit)
        .ToListAsync();

        if (recipes.Count == 0)
            return "Nenhuma receita suficientemente relevante foi encontrada para essa busca.";

        return string.Join("\n", recipes.Select(r => $"- {r.Title} (id {r.Id}): {r.Description} {r.Category.Name} {r.Distance}"));
    }

    [McpServerTool, Description("Busca receitas de um usuário que NÃO contêm um ingrediente específico, ou filtra por categoria. Use para perguntas exatas, tipo 'quais receitas não têm queijo' ou 'quantas receitas de sobremesa eu tenho'.")]
    public async Task<string> SearchRecipesExcludingIngredient(
        [Description("O userId do dono das receitas")] int userId,
        [Description("Ingrediente a excluir, ou vazio para não filtrar")] string? excludeIngredient = null,
        [Description("Filtra receitas por categoria EXATA já cadastrada pelo usuário (ex: 'Sobremesa', 'Salgado') ou excluindo um ingrediente específico. Use SÓ quando o usuário mencionar a categoria explicitamente ou pedir exclusão de ingrediente. Para perguntas sobre sabor, textura ou características gerais (tipo 'algo doce', 'picante', 'leve'), prefira a busca semântica.")] string? categoryName = null)
    {

        var query = _db.Recipes.Where(r => r.UserId == userId);

        if (!string.IsNullOrWhiteSpace(excludeIngredient))
        {
            query = query.Where(r => !r.Ingredients.Any(i => EF.Functions.ILike(i.Description, $"%{excludeIngredient}%")));
        }

        if(!string.IsNullOrWhiteSpace(categoryName))
        {
            query = query.Where(r => r.Category != null && EF.Functions.ILike(r.Category.Name, $"%{categoryName}%"));
        }

        var recipes = await query.Select(r => new { r.Id, r.Title }).ToListAsync();

        if (recipes.Count == 0)
            return "Nenhuma receita encontrada com esse critério.";

        return string.Join("\n", recipes.Select(r => $"- {r.Title} (id {r.Id})"));
    }

    private async Task<Vector?> GenerateEmbeddingAsync(string text)
    {
        var apiKey = _config["Gemini:ApiKey"];
        var payload = new
        {
            content = new { parts = new[] { new { text } } },
            outputDimensionality = 768,
            taskType = "RETRIEVAL_QUERY"
        };

        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent");
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var values = doc.RootElement.GetProperty("embedding").GetProperty("values")
            .EnumerateArray().Select(v => v.GetSingle()).ToArray();

        return new Vector(values);
    }
}