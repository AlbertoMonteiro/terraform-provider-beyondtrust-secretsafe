using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Services.DataSources;
using BeyondTrust.SecretSafeProvider.Tests.Proto;
using Google.Protobuf;
using MessagePack;
using System.Text.Json;

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
        await Assert.That(response.Provider.Block.Attributes).Count().IsEqualTo(4);
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

        // Assert
        await Assert.That(resp.Diagnostics).IsEmpty();
        await Assert.That(resp.State).IsNotNull();

        var responseData = MessagePackSerializer.Deserialize<CredentialData>(resp.State.Msgpack.Memory);
        await Assert.That(responseData.Username).IsEqualTo("service-account");
        await Assert.That(responseData.Password).IsEqualTo("SuperSecret123!");
        await Assert.That(responseData.SecretId).IsEqualTo(credential.SecretId);
    }

    [Test]
    public async Task ReadDataSourceUsingJson_WithCredentialDataRequest_ReturnsSecretFromProvider()
    {
        // Arrange
        CredentialData credential = new() { SecretId = Guid.NewGuid().ToString() };

        ReadDataSource.Types.Request request = new()
        {
            TypeName = CredentialDataSourceHandler.TYPE_NAME,
            Config = new()
            {
                Json = ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes(credential, BeyondTrust.SecretSafeProvider.Serialization.Json.Default.CredentialData)),
                Msgpack = ByteString.Empty,
            }
        };

        // Act
        var resp = await _client.ReadDataSourceAsync(request);

        // Assert
        await Assert.That(resp.Diagnostics).IsEmpty();
        await Assert.That(resp.State).IsNotNull();

        var responseData = MessagePackSerializer.Deserialize<CredentialData>(resp.State.Msgpack.Memory);
        await Assert.That(responseData.Username).IsEqualTo("service-account");
        await Assert.That(responseData.Password).IsEqualTo("SuperSecret123!");
        await Assert.That(responseData.SecretId).IsEqualTo(credential.SecretId);
    }

    [Test]
    public async Task GetSchema_ReturnsFileDownloadDataSourceSchema()
    {
        // Act
        var response = await _client.GetSchemaAsync(new GetProviderSchema.Types.Request());

        // Assert
        await Assert.That(response.DataSourceSchemas.ContainsKey(FileDownloadDataSourceHandler.TYPE_NAME)).IsTrue();
        var attrs = response.DataSourceSchemas[FileDownloadDataSourceHandler.TYPE_NAME].Block.Attributes;
        await Assert.That(attrs).Count().IsEqualTo(3);

        var secretId = attrs.Single(a => a.Name == "secret_id");
        await Assert.That(secretId.Required).IsTrue();
        await Assert.That(secretId.Computed).IsFalse();

        var fileName = attrs.Single(a => a.Name == "file_name");
        await Assert.That(fileName.Computed).IsTrue();
        await Assert.That(fileName.Required).IsFalse();

        var fileContent = attrs.Single(a => a.Name == "file_content_base64");
        await Assert.That(fileContent.Computed).IsTrue();
        await Assert.That(fileContent.Sensitive).IsTrue();
        await Assert.That(fileContent.Required).IsFalse();
    }

    [Test]
    public async Task ReadDataSource_WithFileDownloadRequest_ReturnsFileFromProvider()
    {
        // Arrange
        FileDownloadData fileDownload = new() { SecretId = Guid.NewGuid().ToString() };

        ReadDataSource.Types.Request request = new()
        {
            TypeName = FileDownloadDataSourceHandler.TYPE_NAME,
            Config = new()
            {
                Json = ByteString.Empty,
                Msgpack = ByteString.CopyFrom(MessagePackSerializer.Serialize(fileDownload)),
            }
        };

        // Act
        var resp = await _client.ReadDataSourceAsync(request);

        // Assert
        await Assert.That(resp.Diagnostics).IsEmpty();
        await Assert.That(resp.State).IsNotNull();

        var responseData = MessagePackSerializer.Deserialize<FileDownloadData>(resp.State.Msgpack.Memory);
        var expectedContent = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("secret-file-content-from-wiremock"));
        await Assert.That(responseData.FileContentBase64).IsEqualTo(expectedContent);
        await Assert.That(responseData.FileName).IsEqualTo("secret.txt");
        await Assert.That(responseData.SecretId).IsEqualTo(fileDownload.SecretId);
    }

    [Test]
    public async Task ReadDataSourceUsingJson_WithFileDownloadRequest_ReturnsFileFromProvider()
    {
        // Arrange
        FileDownloadData fileDownload = new() { SecretId = Guid.NewGuid().ToString() };

        ReadDataSource.Types.Request request = new()
        {
            TypeName = FileDownloadDataSourceHandler.TYPE_NAME,
            Config = new()
            {
                Json = ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes(fileDownload, BeyondTrust.SecretSafeProvider.Serialization.Json.Default.FileDownloadData)),
                Msgpack = ByteString.Empty,
            }
        };

        // Act
        var resp = await _client.ReadDataSourceAsync(request);

        // Assert
        await Assert.That(resp.Diagnostics).IsEmpty();
        await Assert.That(resp.State).IsNotNull();

        var responseData = MessagePackSerializer.Deserialize<FileDownloadData>(resp.State.Msgpack.Memory);
        var expectedContent = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("secret-file-content-from-wiremock"));
        await Assert.That(responseData.FileContentBase64).IsEqualTo(expectedContent);
        await Assert.That(responseData.FileName).IsEqualTo("secret.txt");
        await Assert.That(responseData.SecretId).IsEqualTo(fileDownload.SecretId);
    }
}
