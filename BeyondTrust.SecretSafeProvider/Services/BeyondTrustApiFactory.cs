using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Serialization;
using Refit;

namespace BeyondTrust.SecretSafeProvider.Services;

public class BeyondTrustApiFactory(ProviderConfiguration providerConfig, IConfiguration configuration) : IBeyondTrustApiFactory
{
    private readonly ProviderConfiguration _providerConfig = providerConfig;
    private readonly IConfiguration _configuration = configuration;

    private Task<long>? _oauthUserId;

    private Uri BaseAddress => new(_configuration.GetValue<string>("BEYONDTRUST_URL") ?? _providerConfig.BaseUrl);

    public IBeyondTrustSecretSafe CreateApi()
    {
        HttpMessageHandler handler = _providerConfig.UsesOAuth ? CreateOAuthHandler() : CreateHttpHandler();

        HttpClient client = new(handler)
        {
            BaseAddress = BaseAddress,
        };

        var beyondTrustSecretSafe = RestService.For<IBeyondTrustSecretSafe>(client, new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(Json.Default.Options),
            // Sends Content-Length instead of a chunked body
            Buffered = true
        });

        return beyondTrustSecretSafe;
    }

    // The user behind a client_id never changes, so one lookup per provider process is enough.
    public Task<long> GetOAuthUserId()
        => _oauthUserId ??= RequestOAuthUserId();

    private async Task<long> RequestOAuthUserId()
    {
        using var handler = CreateOAuthHandler();

        return await handler.GetClientUserId();
    }

    private OAuthBearerHandler CreateOAuthHandler()
        => new(BaseAddress, _providerConfig.ClientId!, _providerConfig.ClientSecret!, CreateHttpHandler());

    private static HttpClientHandler CreateHttpHandler()
        => new()
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
}
