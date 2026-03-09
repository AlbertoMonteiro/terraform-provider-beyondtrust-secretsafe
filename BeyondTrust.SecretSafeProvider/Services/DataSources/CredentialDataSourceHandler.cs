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
            await secretSafe.SignAppin(new KeyAndRunAs(configuration.Key, configuration.RunAs, configuration.Pwd));
            var secretResponse = await secretSafe.GetSecret(Guid.Parse(input.SecretId));
            await secretSafe.Signout();

            var result = new CredentialData
            {
                SecretId = input.SecretId,
                Username = secretResponse.Username ?? string.Empty,
                Password = secretResponse.Password ?? string.Empty,
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
