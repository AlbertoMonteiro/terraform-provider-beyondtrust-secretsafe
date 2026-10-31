using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BeyondTrust.SecretSafeProvider.Serialization;

namespace BeyondTrust.SecretSafeProvider.Services;

/// <summary>
/// Authenticates every request with an OAuth client credentials token, requested on first use.
/// The instance lives for a single operation, so the token never needs to be refreshed.
/// </summary>
public class OAuthBearerHandler(
    Uri baseAddress,
    string clientId,
    string clientSecret,
    HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
{
    private const string TOKEN_PATH = "/public/v3/Auth/connect/token";
    private const string CLIENT_USER_ID_CLAIM = "_clientUserId";

    private string? _accessToken;

    /// <summary>
    /// Reads the user behind the OAuth client from the token claims. The token comes straight from
    /// the token endpoint, so its signature is not validated.
    /// </summary>
    public async Task<long> GetClientUserId(CancellationToken cancellationToken = default)
    {
        _accessToken ??= await RequestAccessToken(cancellationToken);

        try
        {
            var payload = Base64Url.DecodeFromChars(_accessToken.Split('.').ElementAtOrDefault(1));
            using var claims = JsonDocument.Parse(payload);

            if (claims.RootElement.TryGetProperty(CLIENT_USER_ID_CLAIM, out var claim)
                && long.TryParse(claim.ToString(), out var userId))
                return userId;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            // Not a readable JWT, reported below together with a missing claim.
        }

        throw new InvalidOperationException($"OAuth token has no \"{CLIENT_USER_ID_CLAIM}\" claim. Set owner_id on the resource.");
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _accessToken ??= await RequestAccessToken(cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> RequestAccessToken(CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, $"{baseAddress.AbsoluteUri.TrimEnd('/')}{TOKEN_PATH}")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
            })
        };

        using var response = await base.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"OAuth token request failed with status {(int)response.StatusCode}.", null, response.StatusCode);

        var token = await response.Content.ReadFromJsonAsync(Json.Default.OAuthTokenResponse, cancellationToken);

        if (string.IsNullOrEmpty(token?.AccessToken))
            throw new InvalidOperationException("OAuth token response has no access_token.");

        return token.AccessToken;
    }
}
