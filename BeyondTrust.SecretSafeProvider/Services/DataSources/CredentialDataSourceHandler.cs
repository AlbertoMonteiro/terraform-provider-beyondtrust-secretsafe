using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Serialization;

namespace BeyondTrust.SecretSafeProvider.Services;

public class CredentialDataSourceHandler(
    IBeyondTrustApiFactory apiFactory,
    ProviderConfiguration configuration) : IDataSourceHandler
{
    public string TypeName => "secretsafe_credential_data";

    public Schema GetSchema() => CredentialData.GetSchema();

    public async Task<ReadDataSource.Types.Response> ReadAsync(ReadDataSource.Types.Request request)
    {
        var input = SmartSerializer.Deserialize<CredentialData>(request.Config);

        var secretSafe = apiFactory.CreateApi();

        await secretSafe.SignAppin(new KeyAndRunAs(configuration.Key, configuration.RunAs));
        var secretResponse = await secretSafe.GetSecret(Guid.Parse(input.SecretId));
        await secretSafe.Signout();

        var result = new CredentialData
        {
            SecretId = input.SecretId,
            Username = secretResponse.Content?.Username ?? string.Empty,
            Password = secretResponse.Content?.Password ?? string.Empty,
        };

        return new ReadDataSource.Types.Response
        {
            State = SmartSerializer.Serialize(result)
        };
    }
}
