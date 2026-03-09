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
    public async Task ApplyAsync_WithNoPriorState_CreatesNewFolder()
    {
        // Arrange
        var folderId = Guid.NewGuid().ToString("N");

        var folder = new FolderResourceData
        {
            Name = "Test Folder",
            Description = "Test Description",
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

        var folderResponse = new FolderResponse(folderId, folder.Name, folder.Description, folder.ParentId, folder.UserGroupId);
        // Create request will be verified through the response
        var createRequest = new FolderRequest(folder.Name, folder.Description, folder.ParentId, folder.UserGroupId);

        // Act
        var result = await _sut.ApplyAsync(applyRequest);
        var resultData = SmartSerializer.Deserialize<FolderResourceData>(result.NewState);

        // Assert
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).Called(Count.Once());
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
            UserGroupId = 1
        };

        var plannedFolder = new FolderResourceData
        {
            Id = folderId,
            Name = "New Name",
            Description = "New Description",
            UserGroupId = 1
        };

        var applyRequest = new ApplyResourceChange.Types.Request
        {
            PlannedState = SmartSerializer.Serialize(plannedFolder),
            PriorState = SmartSerializer.Serialize(priorFolder)
        };

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        var folderResponse = new FolderResponse(folderId, plannedFolder.Name, plannedFolder.Description, null, 1);
        var updateRequest = new FolderRequest(plannedFolder.Name, plannedFolder.Description, null, 1);

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
            UserGroupId = 1
        };

        var applyRequest = new ApplyResourceChange.Types.Request
        {
            PlannedState = SmartSerializer.Serialize(folder),
            PriorState = new DynamicValue()
        };

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        var createRequest = new FolderRequest(folder.Name, null, null, 1);

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
