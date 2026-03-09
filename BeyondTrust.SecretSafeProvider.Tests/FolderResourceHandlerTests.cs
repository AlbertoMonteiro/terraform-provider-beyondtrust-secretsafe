using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services;
using BeyondTrust.SecretSafeProvider.Services.Resources;
using Google.Protobuf;
using Imposter.Abstractions;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class FolderResourceHandlerTests
{
    private readonly FolderResourceHandler _sut;
    private readonly IBeyondTrustApiFactoryImposter _beyondTrustApiFactory;
    private readonly ProviderConfiguration _configuration;

    public FolderResourceHandlerTests()
    {
        _beyondTrustApiFactory = IBeyondTrustApiFactory.Imposter();

        _configuration = new ProviderConfiguration
        {
            Key = "",
            RunAs = "",
            BaseUrl = ""
        };

        _sut = new FolderResourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
    }

    [Test]
    public async Task ApplyAsync_WithNoPriorState_CreatesNewFolder_WithDefaultOwner()
    {
        // Arrange - OwnerId is null and should default to authenticated user (42)
        var folderId = Guid.NewGuid().ToString("N");

        var folder = new FolderResourceData
        {
            Name = "Test Folder",
            Description = "Test Description",
            OwnerId = null,
            ParentId = null,
            UserGroupId = 1
        };

        var applyRequest = new ApplyResourceChange.Types.Request
        {
            PlannedState = SmartSerializer.Serialize(folder),
            PriorState = new DynamicValue()
        };

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        var signAppinResponse = new SignAppinResponse(UserId: 42, SID: "test-sid", EmailAddress: "test@example.com", UserName: "testuser", Name: "Test User");
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).ReturnsAsync(signAppinResponse);

        var folderResponse = new FolderResponse(folderId, folder.Name, folder.Description, folder.ParentId, folder.UserGroupId);
        // Handler should create with OwnerId = 42 (from SignAppinResponse.UserId)
        var createRequest = new FolderRequest(
            OwnerId: 42,
            Name: folder.Name,
            Description: folder.Description,
            ParentId: folder.ParentId,
            UserGroupId: folder.UserGroupId);
        imposter.CreateFolder(createRequest).ReturnsAsync(folderResponse);

        // Act
        var result = await _sut.ApplyAsync(applyRequest);
        var resultData = SmartSerializer.Deserialize<FolderResourceData>(result.NewState);

        // Assert
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).Called(Count.Once());
        imposter.CreateFolder(createRequest).Called(Count.Once());
        imposter.Signout().Called(Count.Once());

        await Assert.That(resultData.Id).IsEqualTo(folderId);
        await Assert.That(resultData.Name).IsEqualTo(folder.Name);
        await Assert.That(resultData.UserGroupId).IsEqualTo(1);
    }

    [Test]
    public async Task ApplyAsync_WithNoPriorState_CreatesNewFolder_WithProvidedOwner()
    {
        // Arrange - OwnerId is provided (99) and should override authenticated user (42)
        var folderId = Guid.NewGuid().ToString("N");

        var folder = new FolderResourceData
        {
            Name = "Test Folder",
            Description = "Test Description",
            OwnerId = 99,
            ParentId = null,
            UserGroupId = 1
        };

        var applyRequest = new ApplyResourceChange.Types.Request
        {
            PlannedState = SmartSerializer.Serialize(folder),
            PriorState = new DynamicValue()
        };

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        var signAppinResponse = new SignAppinResponse(UserId: 42, SID: "test-sid", EmailAddress: "test@example.com", UserName: "testuser", Name: "Test User");
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).ReturnsAsync(signAppinResponse);

        var folderResponse = new FolderResponse(folderId, folder.Name, folder.Description, folder.ParentId, folder.UserGroupId);
        // Handler should create with OwnerId = 99 (from FolderResourceData)
        var createRequest = new FolderRequest(
            OwnerId: 99,
            Name: folder.Name,
            Description: folder.Description,
            ParentId: folder.ParentId,
            UserGroupId: folder.UserGroupId);
        imposter.CreateFolder(createRequest).ReturnsAsync(folderResponse);

        // Act
        var result = await _sut.ApplyAsync(applyRequest);
        var resultData = SmartSerializer.Deserialize<FolderResourceData>(result.NewState);

        // Assert
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).Called(Count.Once());
        imposter.CreateFolder(createRequest).Called(Count.Once());
        imposter.Signout().Called(Count.Once());

        await Assert.That(resultData.Id).IsEqualTo(folderId);
        await Assert.That(resultData.Name).IsEqualTo(folder.Name);
        await Assert.That(resultData.UserGroupId).IsEqualTo(1);
    }

    [Test]
    public async Task ApplyAsync_WithPriorState_UpdatesExistingFolder()
    {
        // Arrange
        var folderId = Guid.NewGuid().ToString("N");

        var priorFolder = new FolderResourceData
        {
            Id = folderId,
            Name = "Old Name",
            OwnerId = 42,
            UserGroupId = 1
        };

        var plannedFolder = new FolderResourceData
        {
            Id = folderId,
            Name = "New Name",
            Description = "New Description",
            OwnerId = 42,
            UserGroupId = 1
        };

        var applyRequest = new ApplyResourceChange.Types.Request
        {
            PlannedState = SmartSerializer.Serialize(plannedFolder),
            PriorState = SmartSerializer.Serialize(priorFolder)
        };

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        var signAppinResponse = new SignAppinResponse(UserId: 42, SID: "test-sid", EmailAddress: "test@example.com", UserName: "testuser", Name: "Test User");
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).ReturnsAsync(signAppinResponse);

        var folderResponse = new FolderResponse(folderId, plannedFolder.Name, plannedFolder.Description, null, 1);
        var updateRequest = new FolderRequest(
            OwnerId: plannedFolder.OwnerId,
            Name: plannedFolder.Name,
            Description: plannedFolder.Description,
            ParentId: null,
            UserGroupId: 1);
        imposter.UpdateFolder(folderId, updateRequest).ReturnsAsync(folderResponse);

        // Act
        var result = await _sut.ApplyAsync(applyRequest);
        var resultData = SmartSerializer.Deserialize<FolderResourceData>(result.NewState);

        // Assert
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).Called(Count.Once());
        imposter.Signout().Called(Count.Once());

        await Assert.That(resultData.Name).IsEqualTo(plannedFolder.Name);
    }

    [Test]
    public async Task ReadAsync_WithValidState_ReturnsPopulatedFolderData()
    {
        // Arrange
        var folderId = Guid.NewGuid().ToString("N");

        var folder = new FolderResourceData
        {
            Id = folderId,
            Name = "Test Folder",
            OwnerId = 42,
            UserGroupId = 1
        };

        var readRequest = new ReadResource.Types.Request
        {
            CurrentState = SmartSerializer.Serialize(folder)
        };

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        var folderResponse = new FolderResponse(folderId, folder.Name, null, null, 1);
        imposter.GetFolder(folderId).ReturnsAsync(folderResponse);

        // Act
        var result = await _sut.ReadAsync(readRequest);
        var resultData = SmartSerializer.Deserialize<FolderResourceData>(result.NewState);

        // Assert
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).Called(Count.Once());
        imposter.GetFolder(folderId).Called(Count.Once());
        imposter.Signout().Called(Count.Once());

        await Assert.That(resultData.Id).IsEqualTo(folderId);
        await Assert.That(resultData.Name).IsEqualTo(folder.Name);
    }

    [Test]
    public async Task ApplyAsync_WhenCreateFails_ReturnsDiagnosticWithError()
    {
        // Arrange
        const string exceptionMessage = "Failed to create folder";

        var folder = new FolderResourceData
        {
            Name = "Test Folder",
            OwnerId = null,
            UserGroupId = 1
        };

        var applyRequest = new ApplyResourceChange.Types.Request
        {
            PlannedState = SmartSerializer.Serialize(folder),
            PriorState = new DynamicValue()
        };

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        var signAppinResponse = new SignAppinResponse(UserId: 42, SID: "test-sid", EmailAddress: "test@example.com", UserName: "testuser", Name: "Test User");
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).ReturnsAsync(signAppinResponse);

        var createRequest = new FolderRequest(
            OwnerId: 42,
            Name: folder.Name,
            Description: null,
            ParentId: null,
            UserGroupId: 1);
        imposter.CreateFolder(createRequest)
            .Throws(new Exception(exceptionMessage));

        // Act
        var result = await _sut.ApplyAsync(applyRequest);

        // Assert
        await Assert.That(result.Diagnostics).Count().IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Summary).IsEqualTo("Error");
        await Assert.That(result.Diagnostics[0].Detail).IsEqualTo(exceptionMessage);
    }
}
