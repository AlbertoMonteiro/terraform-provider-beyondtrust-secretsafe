using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services.DataSources;

namespace BeyondTrust.SecretSafeProvider.Services.EphemeralResources;

public class FileDownloadEphemeralResourceHandler(
    IBeyondTrustApiFactory apiFactory,
    ProviderConfiguration configuration) : IEphemeralResourceHandler
{
    public string TypeName => FileDownloadDataSourceHandler.TYPE_NAME;

    public Schema GetSchema() => FileDownloadData.GetSchema();

    public async Task<OpenEphemeralResource.Types.Response> OpenAsync(OpenEphemeralResource.Types.Request request)
    {
        var input = SmartSerializer.Deserialize<FileDownloadData>(request.Config);

        var result = await apiFactory.CreateApi().ReadFile(configuration, input.SecretId);

        return new OpenEphemeralResource.Types.Response
        {
            Result = SmartSerializer.Serialize(result)
        };
    }
}
