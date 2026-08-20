namespace RecipeApp.Application.Common.Interface;

public interface IEmbeddingService
{
    Task<float[]?> GenerateEmbeddingAsync(string text);
}