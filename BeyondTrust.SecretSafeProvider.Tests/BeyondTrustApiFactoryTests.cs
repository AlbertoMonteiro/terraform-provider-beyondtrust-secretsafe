using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Services;
using Microsoft.Extensions.Configuration;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class BeyondTrustApiFactoryTests
{
    private static IConfiguration EmptyConfiguration()
        => new ConfigurationBuilder().Build();

    private static IConfiguration ConfigurationWith(string key, string value)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })
            .Build();

    private static ProviderConfiguration ProviderConfig(string baseUrl = "https://provider.example.com")
        => new() { Key = "key", RunAs = "runAs", BaseUrl = baseUrl };

    [Test]
    public async Task CreateApi_WithProviderConfigUrl_ReturnsNonNullInstance()
    {
        // Arrange
        var sut = new BeyondTrustApiFactory(ProviderConfig(), EmptyConfiguration());

        // Act
        var api = sut.CreateApi();

        // Assert
        await Assert.That(api).IsNotNull();
    }

    [Test]
    public async Task CreateApi_WithConfigurationUrl_ReturnsNonNullInstance()
    {
        // Arrange
        var config = ConfigurationWith("BEYONDTRUST_URL", "https://config.example.com");
        var sut = new BeyondTrustApiFactory(ProviderConfig(), config);

        // Act
        var api = sut.CreateApi();

        // Assert
        await Assert.That(api).IsNotNull();
    }

    [Test]
    public async Task CreateApi_WithOAuthCredentials_ReturnsNonNullInstance()
    {
        // Arrange
        var providerConfig = new ProviderConfiguration
        {
            BaseUrl = "https://provider.example.com",
            ClientId = "id",
            ClientSecret = "secret"
        };
        var sut = new BeyondTrustApiFactory(providerConfig, EmptyConfiguration());

        // Act
        var api = sut.CreateApi();

        // Assert
        await Assert.That(api).IsNotNull();
    }

    [Test]
    public async Task CreateApi_WhenCalledMultipleTimes_ReturnsDistinctInstances()
    {
        // Arrange
        var sut = new BeyondTrustApiFactory(ProviderConfig(), EmptyConfiguration());

        // Act
        var api1 = sut.CreateApi();
        var api2 = sut.CreateApi();

        // Assert
        await Assert.That(api1).IsNotEqualTo(api2);
    }
}
