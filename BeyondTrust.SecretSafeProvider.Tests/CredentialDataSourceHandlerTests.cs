using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services;
using Imposter.Abstractions;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class CredentialDataSourceHandlerTests
{
    private readonly CredentialDataSourceHandler _sut;
    private readonly IBeyondTrustApiFactoryImposter _beyondTrustApiFactory;
    private readonly ProviderConfiguration _configuration;

    public CredentialDataSourceHandlerTests()
    {
        _beyondTrustApiFactory = IBeyondTrustApiFactory.Imposter();

        _configuration = new ProviderConfiguration
        {
            Key = "",
            RunAs = "",
            BaseUrl = ""
        };

        _sut = new CredentialDataSourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
    }

    [Test]
    public async Task ReadAsync_WithValidCredential_ReturnsPopulatedCredentialData()
    {
        //Arrange
        var secretId = Guid.NewGuid();
        CredentialData credential = new()
        {
            SecretId = secretId.ToString("N"),
            Username = "Test",
            Password = "Test"
        };

        ReadDataSource.Types.Request request = new()
        {
             Config = SmartSerializer.Serialize(credential)
        };

        var imposter = IBeyondTrustSecretSafe.Imposter();

        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        var keyAndRunAs = new KeyAndRunAs(_configuration.Key, _configuration.RunAs);

        SecretValue resp = new(credential.Password, credential.Username);

        imposter.GetSecret(secretId).ReturnsAsync(resp);

        //Act
        var result = await _sut.ReadAsync(request);

        var resultData = SmartSerializer.Deserialize<CredentialData>(result.State);

        //Assert
        imposter.SignAppin(keyAndRunAs).Called(Count.Once());
        imposter.GetSecret(secretId).Called(Count.Once());
        imposter.Signout().Called(Count.Once());

        await Assert.That(resultData.Username).IsEqualTo("Test");
        await Assert.That(resultData.Password).IsEqualTo("Test");
        await Assert.That(resultData.SecretId).IsEqualTo(secretId.ToString("N"));
    }

    [Test]
    public async Task ReadAsync_WhenSignAppinThrows_ReturnsDiagnosticWithError()
    {
        // Arrange
        var secretId = Guid.NewGuid();
        var credential = new CredentialData { SecretId = secretId.ToString("N"), Username = "", Password = "" };
        var request = new ReadDataSource.Types.Request { Config = SmartSerializer.Serialize(credential) };
        const string exceptionMessage = "Authentication failed";

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());
        imposter.SignAppin(new KeyAndRunAs(_configuration.Key, _configuration.RunAs))
            .Throws(new Exception(exceptionMessage));

        // Act
        var result = await _sut.ReadAsync(request);

        // Assert
        await Assert.That(result.Diagnostics).Count().IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Summary).IsEqualTo("Error");
        await Assert.That(result.Diagnostics[0].Detail).IsEqualTo(exceptionMessage);
    }

    [Test]
    public async Task ReadAsync_WhenGetSecretThrows_ReturnsDiagnosticWithError()
    {
        // Arrange
        var secretId = Guid.NewGuid();
        var credential = new CredentialData { SecretId = secretId.ToString("N"), Username = "", Password = "" };
        var request = new ReadDataSource.Types.Request { Config = SmartSerializer.Serialize(credential) };
        const string exceptionMessage = "Secret not found";

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());
        imposter.GetSecret(secretId).Throws(new Exception(exceptionMessage));

        // Act
        var result = await _sut.ReadAsync(request);

        // Assert
        await Assert.That(result.Diagnostics).Count().IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Summary).IsEqualTo("Error");
        await Assert.That(result.Diagnostics[0].Detail).IsEqualTo(exceptionMessage);
    }

    [Test]
    public async Task ReadAsync_WhenSignoutThrows_ReturnsDiagnosticWithError()
    {
        // Arrange
        var secretId = Guid.NewGuid();
        var credential = new CredentialData { SecretId = secretId.ToString("N"), Username = "", Password = "" };
        var request = new ReadDataSource.Types.Request { Config = SmartSerializer.Serialize(credential) };
        const string exceptionMessage = "Signout failed";

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());
        imposter.GetSecret(secretId).ReturnsAsync(new SecretValue("password", "username"));
        imposter.Signout().Throws(new Exception(exceptionMessage));

        // Act
        var result = await _sut.ReadAsync(request);

        // Assert
        await Assert.That(result.Diagnostics).Count().IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Summary).IsEqualTo("Error");
        await Assert.That(result.Diagnostics[0].Detail).IsEqualTo(exceptionMessage);
    }
}
