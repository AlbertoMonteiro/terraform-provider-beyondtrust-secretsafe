using System.Text.Json.Serialization;

namespace BeyondTrust.SecretSafeProvider.Models;

public record OAuthTokenResponse(
    [property: JsonPropertyName("access_token")] string? AccessToken);
