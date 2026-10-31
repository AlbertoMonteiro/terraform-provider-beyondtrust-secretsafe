using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services;
using BeyondTrust.SecretSafeProvider.Services.EphemeralResources;
using Imposter.Abstractions;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class CredentialEphemeralResourceHandlerTests
{
    private readonly IBeyondTrustApiFactoryImposter _beyondTrustApiFactory;
    private readonly IBeyondTrustSecretSafeImposter _secretSafe;

    public CredentialEphemeralResourceHandlerTests()
    {
        _secretSafe = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory = IBeyondTrustApiFactory.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(_secretSafe.Instance());
    }

    private CredentialEphemeralResourceHandler CreateSut(ProviderConfiguration configuration)
        => new(_beyondTrustApiFactory.Instance(), configuration);

    private static OpenEphemeralResource.Types.Request OpenRequest(Guid secretId)
        => new()
        {
            TypeName = "secretsafe_credential_data",
            Config = SmartSerializer.Serialize(new CredentialData { SecretId = secretId.ToString("N") })
        };

    [Test]
    public async Task OpenAsync_WithApiKey_ReturnsCredentialAndClosesSession()
    {
        // Arrange
        var secretId = Guid.NewGuid();
        var configuration = new ProviderConfiguration { Key = "key", RunAs = "user", BaseUrl = "" };
        var sut = CreateSut(configuration);

        _secretSafe.GetSecret(secretId).ReturnsAsync(new SecretValue(Username: "service-account", Password: "s3cret"));

        // Act
        var response = await sut.OpenAsync(OpenRequest(secretId));
        var result = SmartSerializer.Deserialize<CredentialData>(response.Result);

        // Assert
        _secretSafe.SignAppin(new KeyAndRunAs("key", "user")).Called(Count.Once());
        _secretSafe.Signout().Called(Count.Once());

        await Assert.That(result.SecretId).IsEqualTo(secretId.ToString("N"));
        await Assert.That(result.Username).IsEqualTo("service-account");
        await Assert.That(result.Password).IsEqualTo("s3cret");
    }

    [Test]
    public async Task OpenAsync_WithOAuth_DoesNotUseSession()
    {
        // Arrange
        var secretId = Guid.NewGuid();
        var configuration = new ProviderConfiguration { ClientId = "client-id", ClientSecret = "client-secret", BaseUrl = "" };
        var sut = CreateSut(configuration);

        _secretSafe.GetSecret(secretId).ReturnsAsync(new SecretValue(Username: "service-account", Password: "s3cret"));

        // Act
        var response = await sut.OpenAsync(OpenRequest(secretId));
        var result = SmartSerializer.Deserialize<CredentialData>(response.Result);

        // Assert
        _secretSafe.SignAppin(Arg<KeyAndRunAs>.Any()).Called(Count.Never());
        _secretSafe.Signout().Called(Count.Never());

        await Assert.That(result.Password).IsEqualTo("s3cret");
    }

    [Test]
    public async Task GetSchema_MatchesCredentialDataSourceAttributes()
    {
        // Arrange
        var sut = CreateSut(new ProviderConfiguration { Key = "", RunAs = "", BaseUrl = "" });

        // Act
        var schema = sut.GetSchema();

        // Assert
        await Assert.That(sut.TypeName).IsEqualTo("secretsafe_credential_data");
        await Assert.That(schema.Block.Attributes.Single(a => a.Name == "password").Sensitive).IsTrue();
        await Assert.That(schema.Block.Attributes).Count().IsEqualTo(3);
    }
}
