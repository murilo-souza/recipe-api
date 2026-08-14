namespace RecipeApp.Application.Common.Interface;

public interface ICloudinaryService
{
    Task<string?> UploadFromUrlAsync(string sourceUrl, string folder);
}