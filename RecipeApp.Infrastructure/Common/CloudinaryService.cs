using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using RecipeApp.Application.Common.Interface;

namespace RecipeApp.Infrastructure.Common;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration config)
    {
        var account = new Account(
            config["Cloudinary:CloudName"],
            config["Cloudinary:ApiKey"],
            config["Cloudinary:ApiSecret"]
        );
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string?> UploadFromUrlAsync(string sourceUrl, string folder)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(sourceUrl), // Cloudinary busca a URL remota sozinho, sem baixar localmente
            Folder = folder,
            Overwrite = false,
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        return result.StatusCode == System.Net.HttpStatusCode.OK
            ? result.SecureUrl?.ToString()
            : null;
    }
}