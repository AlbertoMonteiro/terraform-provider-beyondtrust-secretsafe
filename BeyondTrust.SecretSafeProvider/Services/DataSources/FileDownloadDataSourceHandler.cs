using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;

namespace BeyondTrust.SecretSafeProvider.Services.DataSources;

public class FileDownloadDataSourceHandler(
    IBeyondTrustApiFactory apiFactory,
    ProviderConfiguration configuration) : IDataSourceHandler
{
    public const string TYPE_NAME = "secretsafe_download_file_data";
    public string TypeName => TYPE_NAME;

    public Schema GetSchema() => FileDownloadData.GetSchema();

    public async Task<ReadDataSource.Types.Response> ReadAsync(ReadDataSource.Types.Request request)
    {
        var input = SmartSerializer.Deserialize<FileDownloadData>(request.Config);

        var secretSafe = apiFactory.CreateApi();

        try
        {
            await secretSafe.SignAppin(new KeyAndRunAs(configuration.Key, configuration.RunAs));
            var response = await secretSafe.DownloadSecret(Guid.Parse(input.SecretId));
            await secretSafe.Signout();

            var contentBytes = await response.Content.ReadAsByteArrayAsync();
            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? string.Empty;

            var result = new FileDownloadData
            {
                SecretId = input.SecretId,
                FileName = fileName,
                FileContentBase64 = Convert.ToBase64String(contentBytes),
            };

            return new ReadDataSource.Types.Response
            {
                State = SmartSerializer.Serialize(result)
            };
        }
        catch (Exception ex)
        {
            return new ReadDataSource.Types.Response
            {
                Diagnostics =
                {
                    new Diagnostic()
                    {
                        Detail = ex.Message,
                        Summary = "Error"
                    }
                }
            };
        }
    }
}
