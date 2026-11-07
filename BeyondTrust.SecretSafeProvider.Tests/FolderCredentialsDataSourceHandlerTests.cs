using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services;
using BeyondTrust.SecretSafeProvider.Services.DataSources;
using BeyondTrust.SecretSafeProvider.Services.EphemeralResources;
using Imposter.Abstractions;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class FolderCredentialsDataSourceHandlerTests
{
    private const string FOLDER_ID = "7c77c6d6-a1b2-4c3d-8e9f-0123456789ab";

    private readonly IBeyondTrustApiFactoryImposter _beyondTrustApiFactory;
    private readonly IBeyondTrustSecretSafeImposter _secretSafe;
    private readonly FolderCredentialsDataSourceHandler _sut;

    public FolderCredentialsDataSourceHandlerTests()
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
            TypeName = FolderCredentialsDataSourceHandler.TYPE_NAME,
            Config = SmartSerializer.Serialize(new FolderCredentialsData { FolderId = folderId, FolderName = folderName })
        };

    private static SecretValue Credential(string title, string username, string password)
        => new(Id: Guid.NewGuid().ToString(), Title: title, Username: username, Password: password, SecretType: "Credential",
            ModifiedOn: "2026-10-20T13:45:10.5Z");

    [Test]
    public async Task ReadAsync_WithFolderId_ReturnsCredentialsInStateAndClosesSession()
    {
        // Arrange
        var credential = Credential("db-admin", "dbadmin", "s3cret");

        _secretSafe.GetFolderSecrets(FOLDER_ID).ReturnsAsync(
        [
            credential,
            new SecretValue(Id: Guid.NewGuid().ToString(), Title: "notes", SecretType: "Text"),
        ]);

        // Act
        var response = await _sut.ReadAsync(ReadRequest(folderId: FOLDER_ID));
        var result = SmartSerializer.Deserialize<FolderCredentialsData>(response.State);

        // Assert
        _secretSafe.SignAppin(new KeyAndRunAs("key", "user")).Called(Count.Once());
        _secretSafe.Signout().Called(Count.Once());

        await Assert.That(response.Diagnostics).IsEmpty();
        await Assert.That(result!.FolderId).IsEqualTo(FOLDER_ID);
        await Assert.That(result.Credentials!.Keys).IsEquivalentTo(["db-admin"]);
        await Assert.That(result.Credentials!["db-admin"].ModifiedOn).IsEqualTo("2026-10-20T13:45:10.5Z");
        await Assert.That(result.Credentials!["db-admin"].Password).IsEqualTo("s3cret");
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
        _secretSafe.GetFolderSecrets(FOLDER_ID).ReturnsAsync([Credential("db-admin", "dbadmin", "s3cret")]);

        // Act
        var response = await _sut.ReadAsync(ReadRequest(folderName: "app"));
        var result = SmartSerializer.Deserialize<FolderCredentialsData>(response.State);

        // Assert
        await Assert.That(result!.FolderId).IsEqualTo(FOLDER_ID);
        await Assert.That(result.FolderName).IsEqualTo("app");
    }

    [Test]
    public async Task ReadAsync_WithUnknownFolderName_Throws()
    {
        // Arrange
        _secretSafe.FindFolders("app").ReturnsAsync([]);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ReadAsync(ReadRequest(folderName: "app")));

        // Assert
        _secretSafe.Signout().Called(Count.Once());

        await Assert.That(exception!.Message).IsEqualTo("Folder \"app\" was not found.");
    }

    [Test]
    public async Task GetSchema_IsTheSameAsTheEphemeralResource()
    {
        // Arrange
        var ephemeral = new FolderCredentialsEphemeralResourceHandler(_beyondTrustApiFactory.Instance(), new ProviderConfiguration { BaseUrl = "" });

        // Act
        var schema = _sut.GetSchema();

        // Assert
        await Assert.That(_sut.TypeName).IsEqualTo(ephemeral.TypeName);
        await Assert.That(schema).IsEqualTo(ephemeral.GetSchema());
    }
}
