using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;

namespace BeyondTrust.SecretSafeProvider.Services.DataSources;

// Metadata of the credentials of a folder, without usernames or passwords, so it can feed ordinary
// arguments such as a write-only version without putting any secret in the state.
public class FolderCredentialsMetadataDataSourceHandler(
    IBeyondTrustApiFactory apiFactory,
    ProviderConfiguration configuration) : IDataSourceHandler
{
    public const string TYPE_NAME = "secretsafe_folder_credentials_metadata";
    public string TypeName => TYPE_NAME;

    public Schema GetSchema() => FolderCredentialsMetadataData.GetSchema();

    public async Task<ReadDataSource.Types.Response> ReadAsync(ReadDataSource.Types.Request request)
    {
        var input = SmartSerializer.Deserialize<FolderCredentialsMetadataData>(request.Config);

        var result = await apiFactory.CreateApi().ReadFolderCredentialsMetadata(configuration, input?.FolderId, input?.FolderName);

        return new ReadDataSource.Types.Response
        {
            State = SmartSerializer.Serialize(result)
        };
    }
}
