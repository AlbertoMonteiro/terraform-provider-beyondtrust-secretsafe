using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;

namespace BeyondTrust.SecretSafeProvider.Services.EphemeralResources;

public class FolderCredentialsEphemeralResourceHandler(
    IBeyondTrustApiFactory apiFactory,
    ProviderConfiguration configuration) : IEphemeralResourceHandler
{
    public const string TYPE_NAME = "secretsafe_folder_credentials_data";
    public string TypeName => TYPE_NAME;

    public Schema GetSchema() => FolderCredentialsData.GetSchema();

    public async Task<OpenEphemeralResource.Types.Response> OpenAsync(OpenEphemeralResource.Types.Request request)
    {
        var input = SmartSerializer.Deserialize<FolderCredentialsData>(request.Config);

        var result = await apiFactory.CreateApi().ReadFolderCredentials(configuration, input?.FolderId, input?.FolderName);

        return new OpenEphemeralResource.Types.Response
        {
            Result = SmartSerializer.Serialize(result)
        };
    }
}
