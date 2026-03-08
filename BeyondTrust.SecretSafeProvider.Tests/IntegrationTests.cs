using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Services;
using BeyondTrust.SecretSafeProvider.Tests.Proto;
using Google.Protobuf;
using MessagePack;

namespace BeyondTrust.SecretSafeProvider.Tests;

[ClassDataSource<AspireSetup>(Shared = SharedType.PerAssembly)]
public class IntegrationTests(AspireSetup aspire)
{
    private readonly Provider.ProviderClient _client = aspire.Client;

    [Test]
    public async Task GetSchema_ReturnsProviderSchemaAndCredentialDataSourceSchema()
    {
        // Act
        var response = await _client.GetSchemaAsync(new GetProviderSchema.Types.Request());

        // Assert — provider schema
        await Assert.That(response.Provider).IsNotNull();
        await Assert.That(response.Provider.Block.Attributes).Count().IsEqualTo(3);
        await Assert.That(response.Provider.Block.Attributes.Select(a => a.Name))
            .Contains("key")
            .And.Contains("runas")
            .And.Contains("baseUrl");

        // Assert — credential data source schema
        await Assert.That(response.DataSourceSchemas.ContainsKey(CredentialDataSourceHandler.TYPE_NAME)).IsTrue();
        var attrs = response.DataSourceSchemas[CredentialDataSourceHandler.TYPE_NAME].Block.Attributes;
        await Assert.That(attrs).Count().IsEqualTo(3);

        var secretId = attrs.Single(a => a.Name == "secret_id");
        await Assert.That(secretId.Required).IsTrue();
        await Assert.That(secretId.Computed).IsFalse();

        var username = attrs.Single(a => a.Name == "username");
        await Assert.That(username.Computed).IsTrue();
        await Assert.That(username.Required).IsFalse();

        var password = attrs.Single(a => a.Name == "password");
        await Assert.That(password.Computed).IsTrue();
        await Assert.That(password.Sensitive).IsTrue();
        await Assert.That(password.Required).IsFalse();
    }

    [Test]
    public async Task ReadDataSource_WithCredentialDataRequest_ReturnsSecretFromProvider()
    {
        // Arrange
        CredentialData credential = new() { SecretId = Guid.NewGuid().ToString() };

        ReadDataSource.Types.Request request = new()
        {
            TypeName = CredentialDataSourceHandler.TYPE_NAME,
            Config = new()
            {
                Json = ByteString.Empty,
                Msgpack = ByteString.CopyFrom(MessagePackSerializer.Serialize(credential)),
            }
        };

        // Act
        var resp = await _client.ReadDataSourceAsync(request);
        var responseData = MessagePackSerializer.Deserialize<CredentialData>(resp.State.Msgpack.Memory);

        // Assert
        await Assert.That(responseData.Username).IsEqualTo("service-account");
        await Assert.That(responseData.Password).IsEqualTo("SuperSecret123!");
        await Assert.That(responseData.SecretId).IsEqualTo(credential.SecretId);
    }
}
