using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services;
using BeyondTrust.SecretSafeProvider.Services.EphemeralResources;
using Imposter.Abstractions;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class FolderCredentialsEphemeralResourceHandlerTests
{
    private const string FOLDER_ID = "7c77c6d6-a1b2-4c3d-8e9f-0123456789ab";

    private static readonly ProviderConfiguration OAuthConfiguration = new() { ClientId = "client-id", ClientSecret = "client-secret", BaseUrl = "" };

    private readonly IBeyondTrustApiFactoryImposter _beyondTrustApiFactory;
    private readonly IBeyondTrustSecretSafeImposter _secretSafe;

    public FolderCredentialsEphemeralResourceHandlerTests()
    {
        _secretSafe = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory = IBeyondTrustApiFactory.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(_secretSafe.Instance());
    }

    private FolderCredentialsEphemeralResourceHandler CreateSut(ProviderConfiguration configuration)
        => new(_beyondTrustApiFactory.Instance(), configuration);

    private static OpenEphemeralResource.Types.Request OpenRequest(string? folderId = null, string? folderName = null)
        => new()
        {
            TypeName = FolderCredentialsEphemeralResourceHandler.TYPE_NAME,
            Config = SmartSerializer.Serialize(new FolderCredentialsData { FolderId = folderId, FolderName = folderName })
        };

    private static SecretValue Credential(string title, string username, string password)
        => new(Id: Guid.NewGuid().ToString(), Title: title, Username: username, Password: password, SecretType: "Credential",
            CreatedOn: "2026-10-01T00:00:00Z", CreatedBy: "creator", ModifiedOn: "2026-10-20T13:45:10.5Z", ModifiedBy: "editor");

    [Test]
    public async Task OpenAsync_WithApiKey_ReturnsCredentialsAndClosesSession()
    {
        // Arrange
        var configuration = new ProviderConfiguration { Key = "key", RunAs = "user", BaseUrl = "" };
        var sut = CreateSut(configuration);
        var credential = Credential("db-admin", "dbadmin", "s3cret");

        _secretSafe.GetFolderSecrets(FOLDER_ID).ReturnsAsync([credential]);

        // Act
        var response = await sut.OpenAsync(OpenRequest(folderId: FOLDER_ID));
        var result = SmartSerializer.Deserialize<FolderCredentialsData>(response.Result);

        // Assert
        _secretSafe.SignAppin(new KeyAndRunAs("key", "user")).Called(Count.Once());
        _secretSafe.Signout().Called(Count.Once());
        _secretSafe.FindFolders(Arg<string>.Any()).Called(Count.Never());

        await Assert.That(result!.FolderId).IsEqualTo(FOLDER_ID);
        await Assert.That(result.FolderName).IsNull();
        await Assert.That(result.Credentials!["db-admin"]).IsEqualTo(
            new FolderCredentialEntry(credential.Id!, "dbadmin", "s3cret", "2026-10-01T00:00:00Z", "creator", "2026-10-20T13:45:10.5Z", "editor"));
    }

    [Test]
    public async Task OpenAsync_WithOAuth_DoesNotUseSession()
    {
        // Arrange
        var sut = CreateSut(OAuthConfiguration);

        _secretSafe.GetFolderSecrets(FOLDER_ID).ReturnsAsync([Credential("db-admin", "dbadmin", "s3cret")]);

        // Act
        var response = await sut.OpenAsync(OpenRequest(folderId: FOLDER_ID));
        var result = SmartSerializer.Deserialize<FolderCredentialsData>(response.Result);

        // Assert
        _secretSafe.SignAppin(Arg<KeyAndRunAs>.Any()).Called(Count.Never());
        _secretSafe.Signout().Called(Count.Never());

        await Assert.That(result!.Credentials!["db-admin"].Password).IsEqualTo("s3cret");
    }

    [Test]
    public async Task OpenAsync_WithTextAndFileSecrets_ReturnsOnlyCredentials()
    {
        // Arrange
        var sut = CreateSut(OAuthConfiguration);

        _secretSafe.GetFolderSecrets(FOLDER_ID).ReturnsAsync(
        [
            Credential("db-admin", "dbadmin", "s3cret"),
            new SecretValue(Id: Guid.NewGuid().ToString(), Title: "notes", SecretType: "Text"),
            new SecretValue(Id: Guid.NewGuid().ToString(), Title: "certificate", SecretType: "File"),
            new SecretValue(Id: Guid.NewGuid().ToString(), Title: "api-client", Username: "api", Password: "p4ss", SecretType: "credential"),
        ]);

        // Act
        var response = await sut.OpenAsync(OpenRequest(folderId: FOLDER_ID));
        var result = SmartSerializer.Deserialize<FolderCredentialsData>(response.Result);

        // Assert
        await Assert.That(result!.Credentials!.Keys).IsEquivalentTo(["db-admin", "api-client"]);
    }

    [Test]
    public async Task OpenAsync_WithEmptyFolder_ReturnsEmptyMap()
    {
        // Arrange
        var sut = CreateSut(OAuthConfiguration);

        _secretSafe.GetFolderSecrets(FOLDER_ID).ReturnsAsync([]);

        // Act
        var response = await sut.OpenAsync(OpenRequest(folderId: FOLDER_ID));
        var result = SmartSerializer.Deserialize<FolderCredentialsData>(response.Result);

        // Assert
        await Assert.That(result!.Credentials).IsNotNull();
        await Assert.That(result.Credentials!).IsEmpty();
    }

    [Test]
    public async Task OpenAsync_WithDuplicatedTitle_ThrowsWithoutSecretValues()
    {
        // Arrange
        var configuration = new ProviderConfiguration { Key = "key", RunAs = "user", BaseUrl = "" };
        var sut = CreateSut(configuration);

        _secretSafe.GetFolderSecrets(FOLDER_ID).ReturnsAsync(
        [
            Credential("db-admin", "first-user", "first-password"),
            Credential("db-admin", "second-user", "second-password"),
        ]);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.OpenAsync(OpenRequest(folderId: FOLDER_ID)));

        // Assert
        _secretSafe.Signout().Called(Count.Once());

        await Assert.That(exception!.Message).Contains("\"db-admin\"").And.Contains(FOLDER_ID);
        await Assert.That(exception.Message).DoesNotContain("first-").And.DoesNotContain("second-");
    }

    [Test]
    public async Task OpenAsync_WithFolderName_UsesExactMatchAndReturnsFolderId()
    {
        // Arrange
        var sut = CreateSut(OAuthConfiguration);

        // The API search matches part of the name, so both folders come back
        _secretSafe.FindFolders("APP").ReturnsAsync(
        [
            new FolderResponse("8d88d7e7-b2c3-4d4e-9fa0-1234567890bc", "app-backup", null, null, 1),
            new FolderResponse(FOLDER_ID, "app", null, null, 1),
        ]);
        _secretSafe.GetFolderSecrets(FOLDER_ID).ReturnsAsync([Credential("db-admin", "dbadmin", "s3cret")]);

        // Act
        var response = await sut.OpenAsync(OpenRequest(folderName: "APP"));
        var result = SmartSerializer.Deserialize<FolderCredentialsData>(response.Result);

        // Assert
        _secretSafe.GetFolderSecrets(Arg<string>.Any()).Called(Count.Once());

        await Assert.That(result!.FolderId).IsEqualTo(FOLDER_ID);
        await Assert.That(result.FolderName).IsEqualTo("APP");
        await Assert.That(result.Credentials!.Keys).IsEquivalentTo(["db-admin"]);
    }

    [Test]
    public async Task OpenAsync_WithUnknownFolderName_ThrowsAndClosesSession()
    {
        // Arrange
        var configuration = new ProviderConfiguration { Key = "key", RunAs = "user", BaseUrl = "" };
        var sut = CreateSut(configuration);

        _secretSafe.FindFolders("app").ReturnsAsync([new FolderResponse("8d88d7e7-b2c3-4d4e-9fa0-1234567890bc", "app-backup", null, null, 1)]);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.OpenAsync(OpenRequest(folderName: "app")));

        // Assert
        _secretSafe.Signout().Called(Count.Once());
        _secretSafe.GetFolderSecrets(Arg<string>.Any()).Called(Count.Never());

        await Assert.That(exception!.Message).IsEqualTo("Folder \"app\" was not found.");
    }

    [Test]
    public async Task OpenAsync_WithFolderNameInManyFolders_ThrowsWithFolderIds()
    {
        // Arrange
        var sut = CreateSut(OAuthConfiguration);
        const string otherFolderId = "9e99e8f8-c3d4-4e5f-a0b1-2345678901cd";

        _secretSafe.FindFolders("app").ReturnsAsync(
        [
            new FolderResponse(FOLDER_ID, "app", null, "1a11a1a1-0000-0000-0000-000000000001", 1),
            new FolderResponse(otherFolderId, "App", null, "2b22b2b2-0000-0000-0000-000000000002", 1),
        ]);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.OpenAsync(OpenRequest(folderName: "app")));

        // Assert
        _secretSafe.GetFolderSecrets(Arg<string>.Any()).Called(Count.Never());

        await Assert.That(exception!.Message).Contains(FOLDER_ID).And.Contains(otherFolderId).And.Contains("folder_id");
    }

    [Test]
    [Arguments(FOLDER_ID, "app")]
    [Arguments(null, null)]
    public async Task OpenAsync_WithoutExactlyOneFolderArgument_Throws(string? folderId, string? folderName)
    {
        // Arrange
        var sut = CreateSut(OAuthConfiguration);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => sut.OpenAsync(OpenRequest(folderId, folderName)));

        // Assert
        _secretSafe.FindFolders(Arg<string>.Any()).Called(Count.Never());
        _secretSafe.GetFolderSecrets(Arg<string>.Any()).Called(Count.Never());

        await Assert.That(exception!.Message).IsEqualTo("Set exactly one of \"folder_id\" or \"folder_name\".");
    }

    [Test]
    public async Task GetSchema_HasFolderArgumentsAndSensitiveCredentials()
    {
        // Arrange
        var sut = CreateSut(OAuthConfiguration);

        // Act
        var schema = sut.GetSchema();
        var attributes = schema.Block.Attributes.ToDictionary(a => a.Name);

        // Assert
        await Assert.That(sut.TypeName).IsEqualTo("secretsafe_folder_credentials_data");
        await Assert.That(attributes.Keys).IsEquivalentTo(["folder_id", "folder_name", "credentials"]);
        await Assert.That(attributes["folder_id"].Optional && attributes["folder_id"].Computed).IsTrue();
        await Assert.That(attributes["folder_name"].Optional && !attributes["folder_name"].Computed).IsTrue();
        await Assert.That(attributes["credentials"].Computed && attributes["credentials"].Sensitive).IsTrue();
        await Assert.That(attributes["credentials"].Type.ToStringUtf8())
            .IsEqualTo("""["map",["object",{"secret_id":"string","username":"string","password":"string","created_on":"string","created_by":"string","modified_on":"string","modified_by":"string"}]]""");
    }
}
