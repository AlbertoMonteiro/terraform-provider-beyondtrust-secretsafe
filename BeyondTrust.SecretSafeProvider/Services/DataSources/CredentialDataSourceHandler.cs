using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;

namespace BeyondTrust.SecretSafeProvider.Services.DataSources;

public class CredentialDataSourceHandler(
    IBeyondTrustApiFactory apiFactory,
    ProviderConfiguration configuration) : IDataSourceHandler
{
    public const string TYPE_NAME = "secretsafe_credential_data";
    public string TypeName => TYPE_NAME;

    public Schema GetSchema() => CredentialData.GetSchema();

    public async Task<ReadDataSource.Types.Response> ReadAsync(ReadDataSource.Types.Request request)
    {
        var input = SmartSerializer.Deserialize<CredentialData>(request.Config);

        var secretSafe = apiFactory.CreateApi();

        try
        {
            var result = await secretSafe.ReadCredential(configuration, input.SecretId);

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
                        Severity = Diagnostic.Types.Severity.Error,
                        Detail = ex.Message,
                        Summary = "Error"
                    }
                }
            };
        }
    }
}
