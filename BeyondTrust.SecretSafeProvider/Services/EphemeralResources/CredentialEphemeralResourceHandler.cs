using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services.DataSources;

namespace BeyondTrust.SecretSafeProvider.Services.EphemeralResources;

public class CredentialEphemeralResourceHandler(
    IBeyondTrustApiFactory apiFactory,
    ProviderConfiguration configuration) : IEphemeralResourceHandler
{
    public string TypeName => CredentialDataSourceHandler.TYPE_NAME;

    public Schema GetSchema() => CredentialData.GetSchema();

    public async Task<OpenEphemeralResource.Types.Response> OpenAsync(OpenEphemeralResource.Types.Request request)
    {
        var input = SmartSerializer.Deserialize<CredentialData>(request.Config);

        var result = await apiFactory.CreateApi().ReadCredential(configuration, input.SecretId);

        return new OpenEphemeralResource.Types.Response
        {
            Result = SmartSerializer.Serialize(result)
        };
    }
}
