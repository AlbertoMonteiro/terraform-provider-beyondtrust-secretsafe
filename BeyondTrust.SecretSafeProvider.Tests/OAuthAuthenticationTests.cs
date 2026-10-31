using System.Net;
using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services;
using BeyondTrust.SecretSafeProvider.Services.DataSources;
using BeyondTrust.SecretSafeProvider.Services.Resources;
using Imposter.Abstractions;
using Refit;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class OAuthAuthenticationTests
{
    private const long PLANNED_OWNER_ID = 5;
    private const long TOKEN_USER_ID = 1048;

    private readonly IBeyondTrustApiFactoryImposter _beyondTrustApiFactory;
    private readonly IBeyondTrustSecretSafeImposter _secretSafe;
    private readonly ProviderConfiguration _configuration;

    public OAuthAuthenticationTests()
    {
        _secretSafe = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory = IBeyondTrustApiFactory.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(_secretSafe.Instance());

        _configuration = new ProviderConfiguration
        {
            BaseUrl = "",
            ClientId = "client-id",
            ClientSecret = "client-secret"
        };
    }

    private void AssertNoSessionWasUsed()
    {
        _secretSafe.SignAppin(Arg<KeyAndRunAs>.Any()).Called(Count.Never());
        _secretSafe.Signout().Called(Count.Never());
    }

    private static ApplyResourceChange.Types.Request ApplyRequest<T>(T plannedState)
        => new()
        {
            PlannedState = SmartSerializer.Serialize(plannedState),
            PriorState = new DynamicValue()
        };

    private static FolderCredentialData Credential(long? ownerId)
        => new()
        {
            FolderId = Guid.NewGuid().ToString("N"),
            Title = "Title",
            Username = "user",
            Password = "password",
            OwnerId = ownerId
        };

    private static FolderFileSecretData FileSecret(long? ownerId)
        => new()
        {
            FolderId = Guid.NewGuid().ToString("N"),
            Title = "Title",
            FileName = "file.txt",
            FileContentBase64 = Convert.ToBase64String("content"u8.ToArray()),
            OwnerId = ownerId
        };

    private static FolderResourceData Folder(long? ownerId)
        => new()
        {
            Name = "Folder",
            OwnerId = ownerId,
            UserGroupId = 1
        };

    [Test]
    public async Task CredentialDataSource_WithOAuth_ReadsSecretWithoutSession()
    {
        // Arrange
        var sut = new CredentialDataSourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
        var secretId = Guid.NewGuid();
        var request = new ReadDataSource.Types.Request
        {
            Config = SmartSerializer.Serialize(new CredentialData { SecretId = secretId.ToString("N") })
        };
        _secretSafe.GetSecret(secretId).ReturnsAsync(new SecretValue(Username: "user", Password: "password"));

        // Act
        var result = await sut.ReadAsync(request);
        var resultData = SmartSerializer.Deserialize<CredentialData>(result.State);

        // Assert
        AssertNoSessionWasUsed();
        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(resultData.Username).IsEqualTo("user");
    }

    [Test]
    public async Task FileDownloadDataSource_WithOAuth_DownloadsFileWithoutSession()
    {
        // Arrange
        var sut = new FileDownloadDataSourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
        var secretId = Guid.NewGuid();
        var request = new ReadDataSource.Types.Request
        {
            Config = SmartSerializer.Serialize(new FileDownloadData { SecretId = secretId.ToString("N") })
        };
        _secretSafe.DownloadSecret(secretId).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent("content"u8.ToArray())
        });

        // Act
        var result = await sut.ReadAsync(request);

        // Assert
        AssertNoSessionWasUsed();
        await Assert.That(result.Diagnostics).IsEmpty();
    }

    [Test]
    public async Task FolderCredential_WithOAuthAndOwnerId_CreatesSecretWithPlannedOwner()
    {
        // Arrange
        var sut = new FolderCredentialResourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
        var credential = Credential(PLANNED_OWNER_ID);
        var ownerMatcher = Arg<CreateSecretCredentialRequest>.Is(r => r.OwnerId == PLANNED_OWNER_ID);
        _secretSafe.CreateCredentialSecret(credential.FolderId, ownerMatcher)
            .ReturnsAsync(new SecretResponse(Id: "secret-id"));

        // Act
        var result = await sut.ApplyAsync(ApplyRequest(credential));
        var resultData = SmartSerializer.Deserialize<FolderCredentialData>(result.NewState);

        // Assert
        AssertNoSessionWasUsed();
        _secretSafe.CreateCredentialSecret(credential.FolderId, ownerMatcher).Called(Count.Once());
        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(resultData.OwnerId).IsEqualTo(PLANNED_OWNER_ID);
    }

    [Test]
    public async Task FolderCredential_WithOAuthAndWithoutOwnerId_UsesTokenUserAsOwner()
    {
        // Arrange
        var sut = new FolderCredentialResourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
        var credential = Credential(ownerId: null);
        var ownerMatcher = Arg<CreateSecretCredentialRequest>.Is(r => r.OwnerId == TOKEN_USER_ID);
        _beyondTrustApiFactory.GetOAuthUserId().ReturnsAsync(TOKEN_USER_ID);
        _secretSafe.CreateCredentialSecret(credential.FolderId, ownerMatcher)
            .ReturnsAsync(new SecretResponse(Id: "secret-id", OwnerId: TOKEN_USER_ID));

        // Act
        var result = await sut.ApplyAsync(ApplyRequest(credential));
        var resultData = SmartSerializer.Deserialize<FolderCredentialData>(result.NewState);

        // Assert
        AssertNoSessionWasUsed();
        _secretSafe.CreateCredentialSecret(credential.FolderId, ownerMatcher).Called(Count.Once());
        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(resultData.OwnerId).IsEqualTo(TOKEN_USER_ID);
    }

    [Test]
    public async Task FolderCredential_WithOAuthAndOwnerId_DoesNotLookUpTokenUser()
    {
        // Arrange
        var sut = new FolderCredentialResourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
        var credential = Credential(PLANNED_OWNER_ID);
        _secretSafe.CreateCredentialSecret(Arg<string>.Any(), Arg<CreateSecretCredentialRequest>.Any())
            .ReturnsAsync(new SecretResponse(Id: "secret-id"));

        // Act
        await sut.ApplyAsync(ApplyRequest(credential));

        // Assert
        _beyondTrustApiFactory.GetOAuthUserId().Called(Count.Never());
    }

    [Test]
    public async Task FolderCredential_WhenTokenUserLookupFails_ReturnsDiagnosticAndDoesNotWrite()
    {
        // Arrange
        const string message = "OAuth token has no claim";
        var sut = new FolderCredentialResourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
        _beyondTrustApiFactory.GetOAuthUserId().Throws(new InvalidOperationException(message));

        // Act
        var result = await sut.ApplyAsync(ApplyRequest(Credential(ownerId: null)));

        // Assert
        _secretSafe.CreateCredentialSecret(Arg<string>.Any(), Arg<CreateSecretCredentialRequest>.Any()).Called(Count.Never());
        await Assert.That(result.Diagnostics).Count().IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Detail).IsEqualTo(message);
    }

    [Test]
    public async Task FolderCredential_WithApiKeyAndOwnerId_PrefersPlannedOwnerOverAuthenticatedUser()
    {
        // Arrange
        var configuration = new ProviderConfiguration { BaseUrl = "", Key = "key", RunAs = "user" };
        var sut = new FolderCredentialResourceHandler(_beyondTrustApiFactory.Instance(), configuration);
        var credential = Credential(PLANNED_OWNER_ID);
        var ownerMatcher = Arg<CreateSecretCredentialRequest>.Is(r => r.OwnerId == PLANNED_OWNER_ID);
        _secretSafe.SignAppin(new KeyAndRunAs("key", "user"))
            .ReturnsAsync(new SignAppinResponse(UserId: 42, SID: "sid", EmailAddress: "user@example.com", UserName: "user", Name: "User"));
        _secretSafe.CreateCredentialSecret(credential.FolderId, ownerMatcher)
            .ReturnsAsync(new SecretResponse(Id: "secret-id", OwnerId: 42));

        // Act
        var result = await sut.ApplyAsync(ApplyRequest(credential));
        var resultData = SmartSerializer.Deserialize<FolderCredentialData>(result.NewState);

        // Assert
        _secretSafe.SignAppin(new KeyAndRunAs("key", "user")).Called(Count.Once());
        _secretSafe.Signout().Called(Count.Once());
        _secretSafe.CreateCredentialSecret(credential.FolderId, ownerMatcher).Called(Count.Once());
        await Assert.That(resultData.OwnerId).IsEqualTo(PLANNED_OWNER_ID);
    }

    [Test]
    public async Task FolderFile_WithOAuthAndOwnerId_CreatesSecretWithPlannedOwner()
    {
        // Arrange
        var sut = new FolderFileSecretResourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
        var fileSecret = FileSecret(PLANNED_OWNER_ID);
        var ownerMatcher = Arg<CreateSecretFileRequest>.Is(r => r.OwnerId == PLANNED_OWNER_ID);
        _secretSafe.CreateFileSecret(fileSecret.FolderId, ownerMatcher, Arg<StreamPart>.Any())
            .ReturnsAsync(new SecretFileResponse(Id: "secret-id"));

        // Act
        var result = await sut.ApplyAsync(ApplyRequest(fileSecret));
        var resultData = SmartSerializer.Deserialize<FolderFileSecretData>(result.NewState);

        // Assert
        AssertNoSessionWasUsed();
        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(resultData.OwnerId).IsEqualTo(PLANNED_OWNER_ID);
    }

    [Test]
    public async Task FolderFile_WithOAuthAndWithoutOwnerId_UsesTokenUserAsOwner()
    {
        // Arrange
        var sut = new FolderFileSecretResourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
        var fileSecret = FileSecret(ownerId: null);
        var ownerMatcher = Arg<CreateSecretFileRequest>.Is(r => r.OwnerId == TOKEN_USER_ID);
        _beyondTrustApiFactory.GetOAuthUserId().ReturnsAsync(TOKEN_USER_ID);
        _secretSafe.CreateFileSecret(fileSecret.FolderId, ownerMatcher, Arg<StreamPart>.Any())
            .ReturnsAsync(new SecretFileResponse(Id: "secret-id", OwnerId: TOKEN_USER_ID));

        // Act
        var result = await sut.ApplyAsync(ApplyRequest(fileSecret));
        var resultData = SmartSerializer.Deserialize<FolderFileSecretData>(result.NewState);

        // Assert
        AssertNoSessionWasUsed();
        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(resultData.OwnerId).IsEqualTo(TOKEN_USER_ID);
    }

    [Test]
    public async Task Folder_WithOAuthAndOwnerId_CreatesFolderWithPlannedOwner()
    {
        // Arrange
        var sut = new FolderResourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
        var folder = Folder(PLANNED_OWNER_ID);
        var ownerMatcher = Arg<FolderRequest>.Is(r => r.OwnerId == PLANNED_OWNER_ID);
        _secretSafe.CreateFolder(ownerMatcher)
            .ReturnsAsync(new FolderResponse("folder-id", folder.Name, null, null, folder.UserGroupId));

        // Act
        var result = await sut.ApplyAsync(ApplyRequest(folder));
        var resultData = SmartSerializer.Deserialize<FolderResourceData>(result.NewState);

        // Assert
        AssertNoSessionWasUsed();
        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(resultData.OwnerId).IsEqualTo(PLANNED_OWNER_ID);
    }

    [Test]
    public async Task Folder_WithOAuthAndWithoutOwnerId_UsesTokenUserAsOwner()
    {
        // Arrange
        var sut = new FolderResourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
        var folder = Folder(ownerId: null);
        var ownerMatcher = Arg<FolderRequest>.Is(r => r.OwnerId == TOKEN_USER_ID);
        _beyondTrustApiFactory.GetOAuthUserId().ReturnsAsync(TOKEN_USER_ID);
        _secretSafe.CreateFolder(ownerMatcher)
            .ReturnsAsync(new FolderResponse("folder-id", folder.Name, null, null, null));

        // Act
        var result = await sut.ApplyAsync(ApplyRequest(folder));
        var resultData = SmartSerializer.Deserialize<FolderResourceData>(result.NewState);

        // Assert
        AssertNoSessionWasUsed();
        _secretSafe.CreateFolder(ownerMatcher).Called(Count.Once());
        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(resultData.UserGroupId).IsEqualTo(folder.UserGroupId);
    }
}
