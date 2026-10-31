using BeyondTrust.SecretSafeProvider.Models;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class ProviderConfigurationTests
{
    private static ProviderConfiguration Configuration(
        string? key = null,
        string? runAs = null,
        string? pwd = null,
        string? clientId = null,
        string? clientSecret = null)
        => new() { BaseUrl = "https://example.com", Key = key, RunAs = runAs, Pwd = pwd, ClientId = clientId, ClientSecret = clientSecret };

    [Test]
    public async Task Validate_WithApiKeyAndRunAs_ReturnsNoErrors()
    {
        // Arrange
        var sut = Configuration(key: "key", runAs: "user", pwd: "pwd");

        // Act
        var errors = sut.Validate();

        // Assert
        await Assert.That(errors).IsEmpty();
        await Assert.That(sut.UsesOAuth).IsFalse();
    }

    [Test]
    public async Task Validate_WithClientIdAndClientSecret_ReturnsNoErrors()
    {
        // Arrange
        var sut = Configuration(clientId: "id", clientSecret: "secret");

        // Act
        var errors = sut.Validate();

        // Assert
        await Assert.That(errors).IsEmpty();
        await Assert.That(sut.UsesOAuth).IsTrue();
    }

    [Test]
    public async Task Validate_WithEmptyStringsInUnusedMode_ReturnsNoErrors()
    {
        // Arrange
        var sut = Configuration(key: "", runAs: "", clientId: "id", clientSecret: "secret");

        // Act
        var errors = sut.Validate();

        // Assert
        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task Validate_WithoutAnyCredential_RequiresKeyAndRunAs()
    {
        // Arrange
        var sut = Configuration();

        // Act
        var errors = sut.Validate();

        // Assert
        await Assert.That(errors).Count().IsEqualTo(2);
        await Assert.That(errors.Any(e => e.Contains("\"key\""))).IsTrue();
        await Assert.That(errors.Any(e => e.Contains("\"runas\""))).IsTrue();
    }

    [Test]
    [Arguments("key", null, "\"runas\"")]
    [Arguments(null, "user", "\"key\"")]
    public async Task Validate_WithIncompleteApiKeyPair_ReturnsError(string? key, string? runAs, string expected)
    {
        // Arrange
        var sut = Configuration(key: key, runAs: runAs);

        // Act
        var errors = sut.Validate();

        // Assert
        await Assert.That(errors).Count().IsEqualTo(1);
        await Assert.That(errors[0]).Contains(expected);
    }

    [Test]
    [Arguments("id", null, "\"client_secret\" is required")]
    [Arguments(null, "secret", "\"client_id\" is required")]
    public async Task Validate_WithIncompleteOAuthPair_ReturnsError(string? clientId, string? clientSecret, string expected)
    {
        // Arrange
        var sut = Configuration(clientId: clientId, clientSecret: clientSecret);

        // Act
        var errors = sut.Validate();

        // Assert
        await Assert.That(errors).Count().IsEqualTo(1);
        await Assert.That(errors[0]).Contains(expected);
    }

    [Test]
    [Arguments("key", null, null, "\"key\"")]
    [Arguments(null, "user", null, "\"runas\"")]
    [Arguments(null, null, "pwd", "\"pwd\"")]
    public async Task Validate_MixingOAuthWithApiKeyFields_ReturnsError(string? key, string? runAs, string? pwd, string expected)
    {
        // Arrange
        var sut = Configuration(key: key, runAs: runAs, pwd: pwd, clientId: "id", clientSecret: "secret");

        // Act
        var errors = sut.Validate();

        // Assert
        await Assert.That(errors).Count().IsEqualTo(1);
        await Assert.That(errors[0]).Contains(expected);
    }

    [Test]
    public async Task ReplaceValues_CopiesOAuthCredentials()
    {
        // Arrange
        var sut = Configuration(key: "key", runAs: "user");

        // Act
        sut.ReplaceValues(Configuration(clientId: "id", clientSecret: "secret"));

        // Assert
        await Assert.That(sut.ClientId).IsEqualTo("id");
        await Assert.That(sut.ClientSecret).IsEqualTo("secret");
        await Assert.That(sut.Key).IsNull();
        await Assert.That(sut.RunAs).IsNull();
    }
}
