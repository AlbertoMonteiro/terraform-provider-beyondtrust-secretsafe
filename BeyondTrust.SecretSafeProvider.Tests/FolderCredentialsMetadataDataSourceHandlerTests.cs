using System.Text;
using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services;
using BeyondTrust.SecretSafeProvider.Services.DataSources;
using Imposter.Abstractions;
using MessagePack;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class FolderCredentialsMetadataDataSourceHandlerTests
{
    private const string FOLDER_ID = "7c77c6d6-a1b2-4c3d-8e9f-0123456789ab";

    private readonly IBeyondTrustApiFactoryImposter _beyondTrustApiFactory;
    private readonly IBeyondTrustSecretSafeImposter _secretSafe;
    private readonly FolderCredentialsMetadataDataSourceHandler _sut;

    public FolderCredentialsMetadataDataSourceHandlerTests()
    {
        _secretSafe = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory = IBeyondTrustApiFactory.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(_secretSafe.Instance());

        var configuration = new ProviderConfiguration { Key = "key", RunAs = "user", BaseUrl = "" };
        _sut = new(_beyondTrustApiFactory.Instance(), configuration);
    }

    private static ReadDataSource.Types.Request ReadRequest(string? folderId = null, string? folderName = null)
        => new()
        {
            TypeName = FolderCredentialsMetadataDataSourceHandler.TYPE_NAME,
            Config = SmartSerializer.Serialize(new FolderCredentialsMetadataData { FolderId = folderId, FolderName = folderName })
        };

    private static SecretValue Credential(string title, string username, string password, string modifiedOn)
        => new(Id: Guid.NewGuid().ToString(), Title: title, Username: username, Password: password, SecretType: "Credential",
            CreatedOn: "2026-10-01T00:00:00Z", CreatedBy: "creator", ModifiedOn: modifiedOn, ModifiedBy: "editor");

    [Test]
    public async Task ReadAsync_WithFolderId_ReturnsMetadataKeyedByTitle()
    {
        // Arrange
        var credential = Credential("db-admin", "dbadmin", "s3cret", "2026-10-20T13:45:10.5Z");

        _secretSafe.GetFolderSecrets(FOLDER_ID).ReturnsAsync(
        [
            credential,
            new SecretValue(Id: Guid.NewGuid().ToString(), Title: "notes", SecretType: "Text"),
        ]);

        // Act
        var response = await _sut.ReadAsync(ReadRequest(folderId: FOLDER_ID));
        var result = SmartSerializer.Deserialize<FolderCredentialsMetadataData>(response.State);

        // Assert
        _secretSafe.SignAppin(new KeyAndRunAs("key", "user")).Called(Count.Once());
        _secretSafe.Signout().Called(Count.Once());

        await Assert.That(response.Diagnostics).IsEmpty();
        await Assert.That(result!.FolderId).IsEqualTo(FOLDER_ID);
        await Assert.That(result.Credentials!.Keys).IsEquivalentTo(["db-admin"]);
        await Assert.That(result.Credentials!["db-admin"])
            .IsEqualTo(new FolderCredentialMetadataEntry(credential.Id!, "2026-10-01T00:00:00Z", "creator", "2026-10-20T13:45:10.5Z", "editor"));
    }

    [Test]
    public async Task ReadAsync_StateNeverContainsUsernameOrPassword()
    {
        // Arrange
        _secretSafe.GetFolderSecrets(FOLDER_ID).ReturnsAsync(
        [
            Credential("db-admin", "user-marker-1", "password-marker-1", "2026-10-20T13:45:10.5Z"),
            Credential("api-client", "user-marker-2", "password-marker-2", "2026-10-21T08:30:00Z"),
        ]);

        // Act
        var response = await _sut.ReadAsync(ReadRequest(folderId: FOLDER_ID));
        var json = MessagePackSerializer.ConvertToJson(response.State.Msgpack.Memory);
        var raw = Encoding.UTF8.GetString(response.State.Msgpack.ToByteArray());

        // Assert
        await Assert.That(json).DoesNotContain("marker").And.DoesNotContain("username").And.DoesNotContain("password");
        await Assert.That(raw).DoesNotContain("marker");
    }

    [Test]
    public async Task ReadAsync_WithFolderName_ResolvesFolderId()
    {
        // Arrange
        _secretSafe.FindFolders("app").ReturnsAsync(
        [
            new FolderResponse("8d88d7e7-b2c3-4d4e-9fa0-1234567890bc", "app-backup", null, null, 1),
            new FolderResponse(FOLDER_ID, "app", null, null, 1),
        ]);
        _secretSafe.GetFolderSecrets(FOLDER_ID).ReturnsAsync([Credential("db-admin", "dbadmin", "s3cret", "2026-10-20T13:45:10.5Z")]);

        // Act
        var response = await _sut.ReadAsync(ReadRequest(folderName: "app"));
        var result = SmartSerializer.Deserialize<FolderCredentialsMetadataData>(response.State);

        // Assert
        await Assert.That(result!.FolderId).IsEqualTo(FOLDER_ID);
        await Assert.That(result.FolderName).IsEqualTo("app");
    }

    [Test]
    public async Task ReadAsync_WithSameFolder_HasTheSameKeysAsFolderCredentials()
    {
        // Arrange
        _secretSafe.GetFolderSecrets(FOLDER_ID).ReturnsAsync(
        [
            Credential("db-admin", "dbadmin", "s3cret", "2026-10-20T13:45:10.5Z"),
            Credential("api-client", "api", "p4ss", "2026-10-21T08:30:00Z"),
            new SecretValue(Id: Guid.NewGuid().ToString(), Title: "certificate", SecretType: "File"),
        ]);
        var credentials = new FolderCredentialsDataSourceHandler(_beyondTrustApiFactory.Instance(), new ProviderConfiguration { Key = "key", RunAs = "user", BaseUrl = "" });

        // Act
        var metadata = SmartSerializer.Deserialize<FolderCredentialsMetadataData>((await _sut.ReadAsync(ReadRequest(folderId: FOLDER_ID))).State);
        var full = SmartSerializer.Deserialize<FolderCredentialsData>((await credentials.ReadAsync(new ReadDataSource.Types.Request
        {
            Config = SmartSerializer.Serialize(new FolderCredentialsData { FolderId = FOLDER_ID })
        })).State);

        // Assert
        await Assert.That(metadata!.Credentials!.Keys).IsEquivalentTo(full!.Credentials!.Keys);
    }

    [Test]
    public async Task ReadAsync_WithDuplicatedTitle_Throws()
    {
        // Arrange
        _secretSafe.GetFolderSecrets(FOLDER_ID).ReturnsAsync(
        [
            Credential("db-admin", "first", "first", "2026-10-20T13:45:10.5Z"),
            Credential("db-admin", "second", "second", "2026-10-21T08:30:00Z"),
        ]);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ReadAsync(ReadRequest(folderId: FOLDER_ID)));

        // Assert
        await Assert.That(exception!.Message).Contains("\"db-admin\"");
    }

    [Test]
    [Arguments(FOLDER_ID, "app")]
    [Arguments(null, null)]
    public async Task ReadAsync_WithoutExactlyOneFolderArgument_Throws(string? folderId, string? folderName)
    {
        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.ReadAsync(ReadRequest(folderId, folderName)));

        // Assert
        _secretSafe.GetFolderSecrets(Arg<string>.Any()).Called(Count.Never());

        await Assert.That(exception!.Message).IsEqualTo("Set exactly one of \"folder_id\" or \"folder_name\".");
    }

    [Test]
    public async Task GetSchema_HasNoSensitiveOrSecretAttributes()
    {
        // Act
        var schema = _sut.GetSchema();
        var attributes = schema.Block.Attributes.ToDictionary(a => a.Name);

        // Assert
        await Assert.That(_sut.TypeName).IsEqualTo("secretsafe_folder_credentials_metadata");
        await Assert.That(attributes.Keys).IsEquivalentTo(["folder_id", "folder_name", "credentials"]);
        await Assert.That(attributes.Values.Any(a => a.Sensitive)).IsFalse();
        await Assert.That(attributes["credentials"].Computed).IsTrue();
        await Assert.That(attributes["credentials"].Type.ToStringUtf8())
            .IsEqualTo("""["map",["object",{"secret_id":"string","created_on":"string","created_by":"string","modified_on":"string","modified_by":"string"}]]""");
    }

    [Test]
    public async Task MessagePack_WithNullCredentials_ReadsNullLikeTerraformConfig()
    {
        // Arrange
        var data = new FolderCredentialsMetadataData { FolderName = "app" };

        // Act
        var result = MessagePackSerializer.Deserialize<FolderCredentialsMetadataData>(MessagePackSerializer.Serialize(data));

        // Assert
        await Assert.That(result.FolderName).IsEqualTo("app");
        await Assert.That(result.Credentials).IsNull();
    }
}
