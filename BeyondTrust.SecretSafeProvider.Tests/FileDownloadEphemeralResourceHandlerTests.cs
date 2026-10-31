using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services;
using BeyondTrust.SecretSafeProvider.Services.EphemeralResources;
using Imposter.Abstractions;
using System.Net.Http.Headers;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class FileDownloadEphemeralResourceHandlerTests
{
    private readonly IBeyondTrustApiFactoryImposter _beyondTrustApiFactory;
    private readonly IBeyondTrustSecretSafeImposter _secretSafe;

    public FileDownloadEphemeralResourceHandlerTests()
    {
        _secretSafe = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory = IBeyondTrustApiFactory.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(_secretSafe.Instance());
    }

    private FileDownloadEphemeralResourceHandler CreateSut(ProviderConfiguration configuration)
        => new(_beyondTrustApiFactory.Instance(), configuration);

    private static OpenEphemeralResource.Types.Request OpenRequest(Guid secretId)
        => new()
        {
            TypeName = "secretsafe_download_file_data",
            Config = SmartSerializer.Serialize(new FileDownloadData { SecretId = secretId.ToString("N") })
        };

    private static HttpResponseMessage FileResponse(byte[] content)
    {
        var response = new HttpResponseMessage { Content = new ByteArrayContent(content) };
        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "secret.txt" };
        return response;
    }

    [Test]
    public async Task OpenAsync_WithApiKey_ReturnsFileAndClosesSession()
    {
        // Arrange
        var secretId = Guid.NewGuid();
        var fileContent = "secret-file-content"u8.ToArray();
        var sut = CreateSut(new ProviderConfiguration { Key = "key", RunAs = "user", BaseUrl = "" });

        _secretSafe.DownloadSecret(secretId).ReturnsAsync(FileResponse(fileContent));

        // Act
        var response = await sut.OpenAsync(OpenRequest(secretId));
        var result = SmartSerializer.Deserialize<FileDownloadData>(response.Result);

        // Assert
        _secretSafe.SignAppin(new KeyAndRunAs("key", "user")).Called(Count.Once());
        _secretSafe.Signout().Called(Count.Once());

        await Assert.That(result.SecretId).IsEqualTo(secretId.ToString("N"));
        await Assert.That(result.FileName).IsEqualTo("secret.txt");
        await Assert.That(result.FileContentBase64).IsEqualTo(Convert.ToBase64String(fileContent));
    }

    [Test]
    public async Task OpenAsync_WithOAuth_DoesNotUseSession()
    {
        // Arrange
        var secretId = Guid.NewGuid();
        var fileContent = "secret-file-content"u8.ToArray();
        var sut = CreateSut(new ProviderConfiguration { ClientId = "client-id", ClientSecret = "client-secret", BaseUrl = "" });

        _secretSafe.DownloadSecret(secretId).ReturnsAsync(FileResponse(fileContent));

        // Act
        var response = await sut.OpenAsync(OpenRequest(secretId));
        var result = SmartSerializer.Deserialize<FileDownloadData>(response.Result);

        // Assert
        _secretSafe.SignAppin(Arg<KeyAndRunAs>.Any()).Called(Count.Never());
        _secretSafe.Signout().Called(Count.Never());

        await Assert.That(result.FileContentBase64).IsEqualTo(Convert.ToBase64String(fileContent));
    }

    [Test]
    public async Task GetSchema_MatchesFileDownloadDataSourceAttributes()
    {
        // Arrange
        var sut = CreateSut(new ProviderConfiguration { Key = "", RunAs = "", BaseUrl = "" });

        // Act
        var schema = sut.GetSchema();

        // Assert
        await Assert.That(sut.TypeName).IsEqualTo("secretsafe_download_file_data");
        await Assert.That(schema.Block.Attributes.Single(a => a.Name == "file_content_base64").Sensitive).IsTrue();
        await Assert.That(schema.Block.Attributes).Count().IsEqualTo(3);
    }
}
