using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services;
using BeyondTrust.SecretSafeProvider.Services.Resources;
using Google.Protobuf;
using Imposter.Abstractions;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class FolderCredentialResourceHandlerTests
{
    private readonly FolderCredentialResourceHandler _sut;
    private readonly IBeyondTrustApiFactoryImposter _beyondTrustApiFactory;
    private readonly ProviderConfiguration _configuration;

    public FolderCredentialResourceHandlerTests()
    {
        _beyondTrustApiFactory = IBeyondTrustApiFactory.Imposter();

        _configuration = new ProviderConfiguration
        {
            Key = "",
            RunAs = "",
            BaseUrl = ""
        };

        _sut = new FolderCredentialResourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
    }

    [Test]
    public async Task ApplyAsync_WithNoProiorState_CreatesNewCredentialSecret()
    {
        // Arrange
        var folderId = Guid.NewGuid().ToString("N");
        var secretId = Guid.NewGuid().ToString("N");
        var userId = 42;

        var credential = new FolderCredentialData
        {
            FolderId = folderId,
            Title = "Test Credential",
            Description = "Test Description",
            Username = "testuser",
            Password = "testpass",
            OwnerId = userId
        };

        var applyRequest = new ApplyResourceChange.Types.Request
        {
            PlannedState = SmartSerializer.Serialize(credential),
            PriorState = new DynamicValue()
        };

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        var signAppinResponse = new SignAppinResponse(UserId: userId, SID: "test-sid", EmailAddress: "test@example.com", UserName: "testuser", Name: "Test User");
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).ReturnsAsync(signAppinResponse);

        var secretResponse = new SecretResponse(secretId, credential.Title, credential.Description, userId);
        var createRequest = new CreateSecretCredentialRequest(
            credential.Title,
            credential.Description,
            credential.Username,
            credential.Password,
            userId);
        imposter.CreateCredentialSecret(folderId, createRequest).ReturnsAsync(secretResponse);

        // Act
        var result = await _sut.ApplyAsync(applyRequest);
        var resultData = SmartSerializer.Deserialize<FolderCredentialData>(result.NewState);

        // Assert
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).Called(Count.Once());
        imposter.CreateCredentialSecret(folderId, createRequest).Called(Count.Once());
        imposter.Signout().Called(Count.Once());

        await Assert.That(resultData.Id).IsEqualTo(secretId);
        await Assert.That(resultData.Title).IsEqualTo(credential.Title);
        await Assert.That(resultData.Username).IsEqualTo(credential.Username);
        await Assert.That(resultData.Password).IsEqualTo(credential.Password);
        await Assert.That(resultData.OwnerId).IsEqualTo(userId);
    }

    [Test]
    public async Task ApplyAsync_WithPriorState_UpdatesExistingCredentialSecret()
    {
        // Arrange
        var secretId = Guid.NewGuid().ToString("N");
        var folderId = Guid.NewGuid().ToString("N");
        var userId = 42;

        var priorCredential = new FolderCredentialData
        {
            Id = secretId,
            FolderId = folderId,
            Title = "Old Title",
            Username = "olduser",
            Password = "oldpass",
            OwnerId = userId
        };

        var plannedCredential = new FolderCredentialData
        {
            Id = secretId,
            FolderId = folderId,
            Title = "New Title",
            Description = "New Description",
            Username = "newuser",
            Password = "newpass",
            OwnerId = userId
        };

        var applyRequest = new ApplyResourceChange.Types.Request
        {
            PlannedState = SmartSerializer.Serialize(plannedCredential),
            PriorState = SmartSerializer.Serialize(priorCredential)
        };

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        var signAppinResponse = new SignAppinResponse(UserId: userId, SID: "test-sid", EmailAddress: "test@example.com", UserName: "testuser", Name: "Test User");
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).ReturnsAsync(signAppinResponse);

        var secretResponse = new SecretResponse(secretId, plannedCredential.Title, plannedCredential.Description, userId);
        var updateRequest = new CreateSecretCredentialRequest(
            plannedCredential.Title,
            plannedCredential.Description,
            plannedCredential.Username,
            plannedCredential.Password,
            userId);
        imposter.UpdateCredentialSecret(secretId, updateRequest).ReturnsAsync(secretResponse);

        // Act
        var result = await _sut.ApplyAsync(applyRequest);
        var resultData = SmartSerializer.Deserialize<FolderCredentialData>(result.NewState);

        // Assert
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).Called(Count.Once());
        imposter.UpdateCredentialSecret(secretId, updateRequest).Called(Count.Once());
        imposter.Signout().Called(Count.Once());

        await Assert.That(resultData.Id).IsEqualTo(secretId);
        await Assert.That(resultData.Title).IsEqualTo(plannedCredential.Title);
        await Assert.That(resultData.Username).IsEqualTo(plannedCredential.Username);
    }

    [Test]
    public async Task ReadAsync_WithValidState_ReturnsPopulatedCredentialData()
    {
        // Arrange
        var secretId = Guid.NewGuid().ToString("N");
        var folderId = Guid.NewGuid().ToString("N");
        var userId = 42;

        var credential = new FolderCredentialData
        {
            Id = secretId,
            FolderId = folderId,
            Title = "Test Credential",
            Username = "testuser",
            Password = "testpass",
            OwnerId = userId
        };

        var readRequest = new ReadResource.Types.Request
        {
            CurrentState = SmartSerializer.Serialize(credential)
        };

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        var signAppinResponse = new SignAppinResponse(UserId: userId, SID: "test-sid", EmailAddress: "test@example.com", UserName: "testuser", Name: "Test User");
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).ReturnsAsync(signAppinResponse);

        var secretValue = new SecretValue(credential.Password, credential.Username);
        imposter.GetSecret(Guid.Parse(secretId)).ReturnsAsync(secretValue);

        // Act
        var result = await _sut.ReadAsync(readRequest);
        var resultData = SmartSerializer.Deserialize<FolderCredentialData>(result.NewState);

        // Assert
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).Called(Count.Once());
        imposter.GetSecret(Guid.Parse(secretId)).Called(Count.Once());
        imposter.Signout().Called(Count.Once());

        await Assert.That(resultData.Id).IsEqualTo(secretId);
        await Assert.That(resultData.Username).IsEqualTo(credential.Username);
        await Assert.That(resultData.Password).IsEqualTo(credential.Password);
    }

    [Test]
    public async Task ApplyAsync_WhenCreateFails_ReturnsDiagnosticWithError()
    {
        // Arrange
        var folderId = Guid.NewGuid().ToString("N");
        var userId = 42;
        const string exceptionMessage = "Failed to create credential";

        var credential = new FolderCredentialData
        {
            FolderId = folderId,
            Title = "Test Credential",
            Username = "testuser",
            Password = "testpass",
            OwnerId = userId
        };

        var applyRequest = new ApplyResourceChange.Types.Request
        {
            PlannedState = SmartSerializer.Serialize(credential),
            PriorState = new DynamicValue()
        };

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        var signAppinResponse = new SignAppinResponse(UserId: userId, SID: "test-sid", EmailAddress: "test@example.com", UserName: "testuser", Name: "Test User");
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs)).ReturnsAsync(signAppinResponse);

        var createRequest = new CreateSecretCredentialRequest(
            credential.Title,
            credential.Description,
            credential.Username,
            credential.Password,
            userId);
        imposter.CreateCredentialSecret(folderId, createRequest)
            .Throws(new Exception(exceptionMessage));

        // Act
        var result = await _sut.ApplyAsync(applyRequest);

        // Assert
        await Assert.That(result.Diagnostics).Count().IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Summary).IsEqualTo("Error");
        await Assert.That(result.Diagnostics[0].Detail).IsEqualTo(exceptionMessage);
    }

    [Test]
    public async Task ReadAsync_WhenSignAppinThrows_ReturnsDiagnosticWithError()
    {
        // Arrange
        var secretId = Guid.NewGuid().ToString("N");
        var userId = 42;
        const string exceptionMessage = "Authentication failed";

        var credential = new FolderCredentialData
        {
            Id = secretId,
            FolderId = Guid.NewGuid().ToString("N"),
            Title = "Test Credential",
            Username = "testuser",
            Password = "testpass",
            OwnerId = userId
        };

        var readRequest = new ReadResource.Types.Request
        {
            CurrentState = SmartSerializer.Serialize(credential)
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
}
