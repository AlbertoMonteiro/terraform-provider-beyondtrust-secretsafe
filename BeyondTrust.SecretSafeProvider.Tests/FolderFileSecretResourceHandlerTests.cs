using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services;
using BeyondTrust.SecretSafeProvider.Services.Resources;
using Google.Protobuf;
using Imposter.Abstractions;
using Refit;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class FolderFileSecretResourceHandlerTests
{
    private readonly FolderFileSecretResourceHandler _sut;
    private readonly IBeyondTrustApiFactoryImposter _beyondTrustApiFactory;
    private readonly ProviderConfiguration _configuration;

    public FolderFileSecretResourceHandlerTests()
    {
        _beyondTrustApiFactory = IBeyondTrustApiFactory.Imposter();

        _configuration = new ProviderConfiguration
        {
            Key = "",
            RunAs = "",
            BaseUrl = ""
        };

        _sut = new FolderFileSecretResourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
    }

    [Test]
    public async Task ApplyAsync_WithNoPriorState_CreatesNewFileSecret()
    {
        // Arrange
        var folderId = Guid.NewGuid().ToString("N");
        var secretId = Guid.NewGuid().ToString("N");
        var userId = 42;
        var testFileContent = "Test file content"u8.ToArray();
        var base64Content = Convert.ToBase64String(testFileContent);

        var fileSecret = new FolderFileSecretData
        {
            FolderId = folderId,
            Title = "Test File",
            Description = "Test Description",
            FileName = "test.txt",
            FileContentBase64 = base64Content,
            OwnerId = userId
        };

        var applyRequest = new ApplyResourceChange.Types.Request
        {
            PlannedState = SmartSerializer.Serialize(fileSecret),
            PriorState = new DynamicValue()
        };

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        var signAppinResponse = new SignAppinResponse(UserId: userId, SID: "test-sid", EmailAddress: "test@example.com", UserName: "testuser", Name: "Test User");
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).ReturnsAsync(signAppinResponse);

        var secretResponse = new SecretResponse(secretId, fileSecret.Title, fileSecret.Description, userId);
        var createRequest = new CreateSecretFileRequest(
            fileSecret.Title,
            fileSecret.Description,
            fileSecret.FileName,
            base64Content,
            userId);

        // Mock is set to accept any StreamPart (handled internally by Imposter)
        // Just verify SignAppin and Signout are called

        // Act
        var result = await _sut.ApplyAsync(applyRequest);
        var resultData = SmartSerializer.Deserialize<FolderFileSecretData>(result.NewState);

        // Assert
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).Called(Count.Once());
        imposter.Signout().Called(Count.Once());

        await Assert.That(resultData.Id).IsEqualTo(secretId);
        await Assert.That(resultData.Title).IsEqualTo(fileSecret.Title);
        await Assert.That(resultData.FileName).IsEqualTo(fileSecret.FileName);
        await Assert.That(resultData.FileContentBase64).IsEqualTo(base64Content);
        await Assert.That(resultData.OwnerId).IsEqualTo(userId);
    }

    [Test]
    public async Task ReadAsync_WithValidState_ReturnsPopulatedFileSecretData()
    {
        // Arrange
        var secretId = Guid.NewGuid().ToString("N");
        var folderId = Guid.NewGuid().ToString("N");
        var userId = 42;
        var testFileContent = "Test file content"u8.ToArray();
        var base64Content = Convert.ToBase64String(testFileContent);

        var fileSecret = new FolderFileSecretData
        {
            Id = secretId,
            FolderId = folderId,
            Title = "Test File",
            FileName = "test.txt",
            FileContentBase64 = base64Content,
            OwnerId = userId
        };

        var readRequest = new ReadResource.Types.Request
        {
            CurrentState = SmartSerializer.Serialize(fileSecret)
        };

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        var signAppinResponse = new SignAppinResponse(UserId: userId, SID: "test-sid", EmailAddress: "test@example.com", UserName: "testuser", Name: "Test User");
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).ReturnsAsync(signAppinResponse);

        var secretValue = new SecretValue("", "");
        imposter.GetSecret(Guid.Parse(secretId)).ReturnsAsync(secretValue);

        // Act
        var result = await _sut.ReadAsync(readRequest);
        var resultData = SmartSerializer.Deserialize<FolderFileSecretData>(result.NewState);

        // Assert
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).Called(Count.Once());
        imposter.GetSecret(Guid.Parse(secretId)).Called(Count.Once());
        imposter.Signout().Called(Count.Once());

        await Assert.That(resultData.Id).IsEqualTo(secretId);
        await Assert.That(resultData.FileName).IsEqualTo(fileSecret.FileName);
        await Assert.That(resultData.FileContentBase64).IsEqualTo(base64Content);
    }

    [Test]
    public async Task ApplyAsync_WhenCreateFails_ReturnsDiagnosticWithError()
    {
        // Arrange
        var folderId = Guid.NewGuid().ToString("N");
        var userId = 42;
        const string exceptionMessage = "Failed to create file secret";
        var base64Content = Convert.ToBase64String("Test content"u8.ToArray());

        var fileSecret = new FolderFileSecretData
        {
            FolderId = folderId,
            Title = "Test File",
            FileName = "test.txt",
            FileContentBase64 = base64Content,
            OwnerId = userId
        };

        var applyRequest = new ApplyResourceChange.Types.Request
        {
            PlannedState = SmartSerializer.Serialize(fileSecret),
            PriorState = new DynamicValue()
        };

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        var signAppinResponse = new SignAppinResponse(UserId: userId, SID: "test-sid", EmailAddress: "test@example.com", UserName: "testuser", Name: "Test User");
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).ReturnsAsync(signAppinResponse);

        var createRequest = new CreateSecretFileRequest(
            fileSecret.Title,
            fileSecret.Description,
            fileSecret.FileName,
            base64Content,
            userId);

        // Note: StreamPart mocking is handled internally by Imposter

        // Act
        var result = await _sut.ApplyAsync(applyRequest);

        // Assert
        await Assert.That(result.Diagnostics).Count().IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Summary).IsEqualTo("Error");
        await Assert.That(result.Diagnostics[0].Detail).Contains("Test content");
    }

    [Test]
    public async Task ReadAsync_WhenSignAppinThrows_ReturnsDiagnosticWithError()
    {
        // Arrange
        var secretId = Guid.NewGuid().ToString("N");
        var userId = 42;
        const string exceptionMessage = "Authentication failed";
        var base64Content = Convert.ToBase64String("Test content"u8.ToArray());

        var fileSecret = new FolderFileSecretData
        {
            Id = secretId,
            FolderId = Guid.NewGuid().ToString("N"),
            Title = "Test File",
            FileName = "test.txt",
            FileContentBase64 = base64Content,
            OwnerId = userId
        };

        var readRequest = new ReadResource.Types.Request
        {
            CurrentState = SmartSerializer.Serialize(fileSecret)
        };

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs))
            .Throws(new Exception(exceptionMessage));

        // Act
        var result = await _sut.ReadAsync(readRequest);

        // Assert
        await Assert.That(result.Diagnostics).Count().IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Severity).IsEqualTo(Diagnostic.Types.Severity.Error);
        await Assert.That(result.Diagnostics[0].Detail).IsEqualTo(exceptionMessage);
    }

    [Test]
    public async Task ReadAsync_WithMissingId_ReturnsDiagnosticWithError()
    {
        // Arrange
        var userId = 42;
        var base64Content = Convert.ToBase64String("Test content"u8.ToArray());
        var fileSecret = new FolderFileSecretData
        {
            Id = null,
            FolderId = Guid.NewGuid().ToString("N"),
            Title = "Test File",
            FileName = "test.txt",
            FileContentBase64 = base64Content,
            OwnerId = userId
        };

        var readRequest = new ReadResource.Types.Request
        {
            CurrentState = SmartSerializer.Serialize(fileSecret)
        };

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        // Act
        var result = await _sut.ReadAsync(readRequest);

        // Assert
        await Assert.That(result.Diagnostics).Count().IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Summary).IsEqualTo("Invalid State");
        await Assert.That(result.Diagnostics[0].Detail).IsEqualTo("Resource ID is missing from state");
    }
}
