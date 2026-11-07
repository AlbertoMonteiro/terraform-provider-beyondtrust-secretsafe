using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;

namespace BeyondTrust.SecretSafeProvider.Services.DataSources;

// Same schema and lookup as the ephemeral resource, for places that do not accept ephemeral values.
// Terraform stores every password of the folder in the state.
public class FolderCredentialsDataSourceHandler(
    IBeyondTrustApiFactory apiFactory,
    ProviderConfiguration configuration) : IDataSourceHandler
{
    public const string TYPE_NAME = "secretsafe_folder_credentials_data";
    public string TypeName => TYPE_NAME;

    public Schema GetSchema() => FolderCredentialsData.GetSchema();

    public async Task<ReadDataSource.Types.Response> ReadAsync(ReadDataSource.Types.Request request)
    {
        var input = SmartSerializer.Deserialize<FolderCredentialsData>(request.Config);

        var result = await apiFactory.CreateApi().ReadFolderCredentials(configuration, input?.FolderId, input?.FolderName);

        return new ReadDataSource.Types.Response
        {
            State = SmartSerializer.Serialize(result)
        };
    }
}
