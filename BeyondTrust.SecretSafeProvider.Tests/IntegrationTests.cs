using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Services.DataSources;
using BeyondTrust.SecretSafeProvider.Services.EphemeralResources;
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
        await Assert.That(response.Provider.Block.Attributes).Count().IsEqualTo(7);
        await Assert.That(response.Provider.Block.Attributes.Select(a => a.Name))
            .Contains("key")
            .And.Contains("runas")
            .And.Contains("baseUrl")
            .And.Contains("client_id")
            .And.Contains("client_secret")
            .And.Contains("insecure");
        await Assert.That(response.Provider.Block.Attributes.Single(a => a.Name == "client_secret").Sensitive).IsTrue();
        await Assert.That(response.Provider.Block.Attributes.Single(a => a.Name == "key").Required).IsFalse();

        // Assert — credential data source schema
        await Assert.That(response.DataSourceSchemas.ContainsKey(CredentialDataSourceHandler.TYPE_NAME)).IsTrue();
        var attrs = response.DataSourceSchemas[CredentialDataSourceHandler.TYPE_NAME].Block.Attributes;
        await Assert.That(attrs).Count().IsEqualTo(15);

        var modifiedOn = attrs.Single(a => a.Name == "modified_on");
        await Assert.That(modifiedOn.Computed).IsTrue();
        await Assert.That(modifiedOn.Sensitive).IsFalse();

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
        await Assert.That(responseData.Title).IsEqualTo("My Test Secret");
        await Assert.That(responseData.ModifiedOn).IsEqualTo("2026-10-20T13:45:10.5Z");
        await Assert.That(responseData.ModifiedBy).IsEqualTo("admin");
        await Assert.That(responseData.OwnerId).IsEqualTo(1);
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
        await Assert.That(attrs).Count().IsEqualTo(16);
        await Assert.That(attrs.Single(a => a.Name == "file_hash").Computed).IsTrue();

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
        await Assert.That(responseData.ModifiedOn).IsEqualTo("2026-10-20T13:45:10.5Z");
    }

    [Test]
    public async Task GetSchema_ReturnsEphemeralResourceSchemas()
    {
        // Act
        var response = await _client.GetSchemaAsync(new GetProviderSchema.Types.Request());

        // Assert
        await Assert.That(response.EphemeralResourceSchemas.ContainsKey(CredentialDataSourceHandler.TYPE_NAME)).IsTrue();
        await Assert.That(response.EphemeralResourceSchemas.ContainsKey(FileDownloadDataSourceHandler.TYPE_NAME)).IsTrue();

        var password = response.EphemeralResourceSchemas[CredentialDataSourceHandler.TYPE_NAME].Block.Attributes.Single(a => a.Name == "password");
        await Assert.That(password.Computed).IsTrue();
        await Assert.That(password.Sensitive).IsTrue();
    }

    [Test]
    public async Task OpenEphemeralResource_WithCredentialRequest_ReturnsSecretFromProvider()
    {
        // Arrange
        CredentialData credential = new() { SecretId = Guid.NewGuid().ToString() };

        OpenEphemeralResource.Types.Request request = new()
        {
            TypeName = CredentialDataSourceHandler.TYPE_NAME,
            Config = new()
            {
                Msgpack = ByteString.CopyFrom(MessagePackSerializer.Serialize(credential)),
            }
        };

        // Act
        var resp = await _client.OpenEphemeralResourceAsync(request);

        // Assert
        await Assert.That(resp.Diagnostics).IsEmpty();
        await Assert.That(resp.RenewAt).IsNull();

        var responseData = MessagePackSerializer.Deserialize<CredentialData>(resp.Result.Msgpack.Memory);
        await Assert.That(responseData.Username).IsEqualTo("service-account");
        await Assert.That(responseData.Password).IsEqualTo("SuperSecret123!");
        await Assert.That(responseData.SecretId).IsEqualTo(credential.SecretId);
    }

    [Test]
    public async Task GetSchema_ReturnsFolderCredentialsEphemeralResourceSchema()
    {
        // Act
        var response = await _client.GetSchemaAsync(new GetProviderSchema.Types.Request());

        // Assert
        await Assert.That(response.EphemeralResourceSchemas.ContainsKey(FolderCredentialsEphemeralResourceHandler.TYPE_NAME)).IsTrue();

        var credentials = response.EphemeralResourceSchemas[FolderCredentialsEphemeralResourceHandler.TYPE_NAME].Block.Attributes.Single(a => a.Name == "credentials");
        await Assert.That(credentials.Computed).IsTrue();
        await Assert.That(credentials.Sensitive).IsTrue();
    }

    [Test]
    public async Task GetSchema_ReturnsFolderCredentialsDataSourceSchema()
    {
        // Act
        var response = await _client.GetSchemaAsync(new GetProviderSchema.Types.Request());

        // Assert
        await Assert.That(response.DataSourceSchemas.ContainsKey(FolderCredentialsDataSourceHandler.TYPE_NAME)).IsTrue();

        var credentials = response.DataSourceSchemas[FolderCredentialsDataSourceHandler.TYPE_NAME].Block.Attributes.Single(a => a.Name == "credentials");
        await Assert.That(credentials.Computed).IsTrue();
        await Assert.That(credentials.Sensitive).IsTrue();
    }

    [Test]
    [Arguments("7c77c6d6-a1b2-4c3d-8e9f-0123456789ab", null)]
    [Arguments(null, "APP")]
    public async Task ReadDataSource_WithFolderCredentialsRequest_ReturnsOnlyCredentials(string? folderId, string? folderName)
    {
        // Arrange
        FolderCredentialsData folder = new() { FolderId = folderId, FolderName = folderName };

        ReadDataSource.Types.Request request = new()
        {
            TypeName = FolderCredentialsDataSourceHandler.TYPE_NAME,
            Config = new()
            {
                Msgpack = ByteString.CopyFrom(MessagePackSerializer.Serialize(folder)),
            }
        };

        // Act
        var resp = await _client.ReadDataSourceAsync(request);

        // Assert
        await Assert.That(resp.Diagnostics).IsEmpty();

        var responseData = MessagePackSerializer.Deserialize<FolderCredentialsData>(resp.State.Msgpack.Memory);
        await Assert.That(responseData.FolderId).IsEqualTo("7c77c6d6-a1b2-4c3d-8e9f-0123456789ab");
        await Assert.That(responseData.FolderName).IsEqualTo(folderName);
        await Assert.That(responseData.Credentials!.Keys).IsEquivalentTo(["db-admin", "api-client"]);
        await Assert.That(responseData.Credentials!["db-admin"].Password).IsEqualTo("FolderDbSecret123!");
        await Assert.That(responseData.Credentials!["db-admin"].ModifiedOn).IsEqualTo("2026-10-21T08:30:00.25Z");
        await Assert.That(responseData.Credentials!["db-admin"].ModifiedBy).IsEqualTo("folder-admin");
    }

    [Test]
    [Arguments("7c77c6d6-a1b2-4c3d-8e9f-0123456789ab", null)]
    [Arguments(null, "APP")]
    public async Task ReadDataSource_WithFolderCredentialsMetadataRequest_ReturnsMetadataWithoutSecrets(string? folderId, string? folderName)
    {
        // Arrange
        FolderCredentialsMetadataData folder = new() { FolderId = folderId, FolderName = folderName };

        ReadDataSource.Types.Request request = new()
        {
            TypeName = FolderCredentialsMetadataDataSourceHandler.TYPE_NAME,
            Config = new()
            {
                Msgpack = ByteString.CopyFrom(MessagePackSerializer.Serialize(folder)),
            }
        };

        // Act
        var resp = await _client.ReadDataSourceAsync(request);

        // Assert
        await Assert.That(resp.Diagnostics).IsEmpty();

        var responseData = MessagePackSerializer.Deserialize<FolderCredentialsMetadataData>(resp.State.Msgpack.Memory);
        await Assert.That(responseData.FolderId).IsEqualTo("7c77c6d6-a1b2-4c3d-8e9f-0123456789ab");
        await Assert.That(responseData.Credentials!.Keys).IsEquivalentTo(["db-admin", "api-client"]);
        await Assert.That(responseData.Credentials!["db-admin"].ModifiedOn).IsEqualTo("2026-10-21T08:30:00.25Z");
        await Assert.That(MessagePackSerializer.ConvertToJson(resp.State.Msgpack.Memory)).DoesNotContain("FolderDbSecret123!");
    }

    [Test]
    public async Task ReadDataSource_WithFolderIdAndFolderName_ReturnsDiagnostic()
    {
        // Arrange
        FolderCredentialsData folder = new() { FolderId = "7c77c6d6-a1b2-4c3d-8e9f-0123456789ab", FolderName = "app" };

        ReadDataSource.Types.Request request = new()
        {
            TypeName = FolderCredentialsDataSourceHandler.TYPE_NAME,
            Config = new()
            {
                Msgpack = ByteString.CopyFrom(MessagePackSerializer.Serialize(folder)),
            }
        };

        // Act
        var resp = await _client.ReadDataSourceAsync(request);

        // Assert
        await Assert.That(resp.Diagnostics).Count().IsEqualTo(1);
        await Assert.That(resp.Diagnostics[0].Detail).IsEqualTo("Set exactly one of \"folder_id\" or \"folder_name\".");
    }

    [Test]
    [Arguments("7c77c6d6-a1b2-4c3d-8e9f-0123456789ab", null)]
    [Arguments(null, "APP")]
    public async Task OpenEphemeralResource_WithFolderCredentialsRequest_ReturnsOnlyCredentials(string? folderId, string? folderName)
    {
        // Arrange
        FolderCredentialsData folder = new() { FolderId = folderId, FolderName = folderName };

        OpenEphemeralResource.Types.Request request = new()
        {
            TypeName = FolderCredentialsEphemeralResourceHandler.TYPE_NAME,
            Config = new()
            {
                Msgpack = ByteString.CopyFrom(MessagePackSerializer.Serialize(folder)),
            }
        };

        // Act
        var resp = await _client.OpenEphemeralResourceAsync(request);

        // Assert
        await Assert.That(resp.Diagnostics).IsEmpty();

        var responseData = MessagePackSerializer.Deserialize<FolderCredentialsData>(resp.Result.Msgpack.Memory);
        await Assert.That(responseData.FolderId).IsEqualTo("7c77c6d6-a1b2-4c3d-8e9f-0123456789ab");
        await Assert.That(responseData.FolderName).IsEqualTo(folderName);
        await Assert.That(responseData.Credentials!.Keys).IsEquivalentTo(["db-admin", "api-client"]);
        await Assert.That(responseData.Credentials!["db-admin"].Username).IsEqualTo("dbadmin");
        await Assert.That(responseData.Credentials!["db-admin"].Password).IsEqualTo("FolderDbSecret123!");
    }

    [Test]
    public async Task OpenEphemeralResource_WithFileDownloadRequest_ReturnsFileFromProvider()
    {
        // Arrange
        FileDownloadData fileDownload = new() { SecretId = Guid.NewGuid().ToString() };

        OpenEphemeralResource.Types.Request request = new()
        {
            TypeName = FileDownloadDataSourceHandler.TYPE_NAME,
            Config = new()
            {
                Msgpack = ByteString.CopyFrom(MessagePackSerializer.Serialize(fileDownload)),
            }
        };

        // Act
        var resp = await _client.OpenEphemeralResourceAsync(request);

        // Assert
        await Assert.That(resp.Diagnostics).IsEmpty();

        var responseData = MessagePackSerializer.Deserialize<FileDownloadData>(resp.Result.Msgpack.Memory);
        var expectedContent = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("secret-file-content-from-wiremock"));
        await Assert.That(responseData.FileContentBase64).IsEqualTo(expectedContent);
        await Assert.That(responseData.FileName).IsEqualTo("secret.txt");
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
