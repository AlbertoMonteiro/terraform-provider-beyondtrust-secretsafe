using BeyondTrust.SecretSafeProvider.Models;
using Refit;

namespace BeyondTrust.SecretSafeProvider.Services;

public class BeyondTrustApiFactory(ProviderConfiguration providerConfig, IConfiguration configuration) : IBeyondTrustApiFactory
{
    private readonly ProviderConfiguration _providerConfig = providerConfig;
    private readonly IConfiguration _configuration = configuration;

    public IBeyondTrustSecretSafe CreateApi()
    {
        HttpClientHandler handler = new()
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        HttpClient client = new(handler)
        {
            BaseAddress = new Uri(_configuration.GetValue<string>("BEYONDTRUST_URL") ?? _providerConfig.BaseUrl),
        };

        var beyondTrustSecretSafe = RestService.For<IBeyondTrustSecretSafe>(client, new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(Json.Default.Options)
        });

        return beyondTrustSecretSafe;
    }
}
