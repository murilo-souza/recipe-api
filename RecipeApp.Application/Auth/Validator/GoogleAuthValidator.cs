using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using RecipeApp.Application.Auth;

namespace RecipeApp.Infrastructure.Auth;

public class GoogleAuthValidator : IGoogleAuthValidator
{
    private readonly IConfiguration _config;

    public GoogleAuthValidator(IConfiguration config) => _config = config;

    public async Task<GoogleUserInfo?> ValidateAsync(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _config["Google:ClientId"]! }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return new GoogleUserInfo(payload.Subject, payload.Email, payload.Name, payload.Picture);
        }
        catch (InvalidJwtException)
        {
            // Token inválido, expirado, ou audience/assinatura não batem
            return null;
        }
    }
}