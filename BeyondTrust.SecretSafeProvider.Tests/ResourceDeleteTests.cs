using System.Net;
using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services;
using BeyondTrust.SecretSafeProvider.Services.Resources;
using Google.Protobuf;
using Imposter.Abstractions;
using MessagePack;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class ResourceDeleteTests
{
    private const string RESOURCE_ID = "06b17a29-adf3-4b06-8972-08df257f0006";

    private static readonly DynamicValue NullObject = new() { Msgpack = ByteString.CopyFrom(MessagePackCode.Nil) };

    private readonly IBeyondTrustApiFactoryImposter _beyondTrustApiFactory;
    private readonly IBeyondTrustSecretSafeImposter _secretSafe;
    private readonly ProviderConfiguration _configuration;

    public ResourceDeleteTests()
    {
        _secretSafe = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory = IBeyondTrustApiFactory.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(_secretSafe.Instance());

        _configuration = new ProviderConfiguration { BaseUrl = "", Key = "key", RunAs = "user" };
    }

    private static ApplyResourceChange.Types.Request DeleteRequest<T>(T priorState)
        => new()
        {
            PlannedState = NullObject,
            PriorState = SmartSerializer.Serialize(priorState)
        };

    private static FolderCredentialData Credential()
        => new() { Id = RESOURCE_ID, FolderId = "folder", Title = "Title", Username = "user", Password = "password" };

    private static FolderFileSecretData FileSecret()
        => new() { Id = RESOURCE_ID, FolderId = "folder", Title = "Title", FileName = "file.txt", FileContentBase64 = "Y29udGVudA==" };

    private static FolderResourceData Folder()
        => new() { Id = RESOURCE_ID, Name = "Folder", UserGroupId = 1 };

    [Test]
    public async Task FolderCredential_WhenDestroying_DeletesSecret()
    {
        // Arrange
        var sut = new FolderCredentialResourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
        _secretSafe.DeleteSecret(RESOURCE_ID).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var result = await sut.ApplyAsync(DeleteRequest(Credential()));

        // Assert
        _secretSafe.DeleteSecret(RESOURCE_ID).Called(Count.Once());
        _secretSafe.Signout().Called(Count.Once());
        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(result.NewState).IsEqualTo(NullObject);
    }

    [Test]
    public async Task FolderCredential_WhenSecretIsAlreadyGone_Succeeds()
    {
        // Arrange
        var sut = new FolderCredentialResourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
        _secretSafe.DeleteSecret(RESOURCE_ID).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var result = await sut.ApplyAsync(DeleteRequest(Credential()));

        // Assert
        await Assert.That(result.Diagnostics).IsEmpty();
    }

    [Test]
    public async Task FolderCredential_WhenDeleteFails_ReturnsDiagnostic()
    {
        // Arrange
        var sut = new FolderCredentialResourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
        _secretSafe.DeleteSecret(RESOURCE_ID).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Forbidden));

        // Act
        var result = await sut.ApplyAsync(DeleteRequest(Credential()));

        // Assert
        await Assert.That(result.Diagnostics).Count().IsEqualTo(1);
    }

    [Test]
    public async Task FolderFile_WhenDestroying_DeletesSecret()
    {
        // Arrange
        var sut = new FolderFileSecretResourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
        _secretSafe.DeleteSecret(RESOURCE_ID).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var result = await sut.ApplyAsync(DeleteRequest(FileSecret()));

        // Assert
        _secretSafe.DeleteSecret(RESOURCE_ID).Called(Count.Once());
        await Assert.That(result.Diagnostics).IsEmpty();
    }

    [Test]
    public async Task Folder_WhenDestroying_DeletesFolderWithoutWriting()
    {
        // Arrange
        var sut = new FolderResourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
        _secretSafe.DeleteFolder(RESOURCE_ID).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var result = await sut.ApplyAsync(DeleteRequest(Folder()));

        // Assert
        _secretSafe.DeleteFolder(RESOURCE_ID).Called(Count.Once());
        _secretSafe.UpdateFolder(Arg<string>.Any(), Arg<FolderRequest>.Any()).Called(Count.Never());
        _secretSafe.CreateFolder(Arg<FolderRequest>.Any()).Called(Count.Never());
        await Assert.That(result.Diagnostics).IsEmpty();
    }

    [Test]
    public async Task Folder_WhenFolderHasSecrets_ReturnsConflictDiagnostic()
    {
        // Arrange
        var sut = new FolderResourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
        _secretSafe.DeleteFolder(RESOURCE_ID).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict));

        // Act
        var result = await sut.ApplyAsync(DeleteRequest(Folder()));

        // Assert
        await Assert.That(result.Diagnostics).Count().IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Summary).IsEqualTo("Cannot Delete Folder");
    }
}
