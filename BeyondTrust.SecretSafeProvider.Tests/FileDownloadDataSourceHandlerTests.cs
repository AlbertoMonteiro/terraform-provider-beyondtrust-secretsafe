using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services;
using BeyondTrust.SecretSafeProvider.Services.DataSources;
using Imposter.Abstractions;
using System.Net.Http.Headers;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class FileDownloadDataSourceHandlerTests
{
    private readonly FileDownloadDataSourceHandler _sut;
    private readonly IBeyondTrustApiFactoryImposter _beyondTrustApiFactory;
    private readonly ProviderConfiguration _configuration;

    public FileDownloadDataSourceHandlerTests()
    {
        _beyondTrustApiFactory = IBeyondTrustApiFactory.Imposter();

        _configuration = new ProviderConfiguration
        {
            Key = "",
            RunAs = "",
            BaseUrl = ""
        };

        _sut = new FileDownloadDataSourceHandler(_beyondTrustApiFactory.Instance(), _configuration);
    }

    [Test]
    public async Task ReadAsync_WithValidSecret_ReturnsFileContentAndFileName()
    {
        // Arrange
        var secretId = Guid.NewGuid();
        var input = new FileDownloadData
        {
            SecretId = secretId.ToString("N"),
        };

        var request = new ReadDataSource.Types.Request
        {
            Config = SmartSerializer.Serialize(input)
        };

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        var fileContent = "secret-file-content"u8.ToArray();
        var httpResponse = new HttpResponseMessage
        {
            Content = new ByteArrayContent(fileContent)
        };
        httpResponse.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
        {
            FileName = "secret.txt"
        };

        imposter.DownloadSecret(secretId).ReturnsAsync(httpResponse);

        // Act
        var result = await _sut.ReadAsync(request);
        var resultData = SmartSerializer.Deserialize<FileDownloadData>(result.State);

        // Assert
        var keyAndRunAs = new KeyAndRunAs(_configuration.Key, _configuration.RunAs);
        imposter.SignAppin(keyAndRunAs).Called(Count.Once());
        imposter.DownloadSecret(secretId).Called(Count.Once());
        imposter.Signout().Called(Count.Once());

        await Assert.That(resultData.FileName).IsEqualTo("secret.txt");
        await Assert.That(resultData.FileContentBase64).IsEqualTo(Convert.ToBase64String(fileContent));
        await Assert.That(resultData.SecretId).IsEqualTo(secretId.ToString("N"));
    }

    [Test]
    public async Task ReadAsync_WhenSignAppinThrows_ReturnsDiagnosticWithError()
    {
        // Arrange
        var secretId = Guid.NewGuid();
        var input = new FileDownloadData { SecretId = secretId.ToString("N") };
        var request = new ReadDataSource.Types.Request { Config = SmartSerializer.Serialize(input) };
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
    public async Task ReadAsync_WhenDownloadSecretThrows_ReturnsDiagnosticWithError()
    {
        // Arrange
        var secretId = Guid.NewGuid();
        var input = new FileDownloadData { SecretId = secretId.ToString("N") };
        var request = new ReadDataSource.Types.Request { Config = SmartSerializer.Serialize(input) };
        const string exceptionMessage = "File not found";

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());
        imposter.DownloadSecret(secretId).Throws(new Exception(exceptionMessage));

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
        var input = new FileDownloadData { SecretId = secretId.ToString("N") };
        var request = new ReadDataSource.Types.Request { Config = SmartSerializer.Serialize(input) };
        const string exceptionMessage = "Signout failed";

        var imposter = IBeyondTrustSecretSafe.Imposter();
        _beyondTrustApiFactory.CreateApi().Returns(imposter.Instance());

        var httpResponse = new HttpResponseMessage
        {
            Content = new ByteArrayContent("content"u8.ToArray())
        };
        imposter.DownloadSecret(secretId).ReturnsAsync(httpResponse);
        imposter.Signout().Throws(new Exception(exceptionMessage));

        // Act
        var result = await _sut.ReadAsync(request);

        // Assert
        await Assert.That(result.Diagnostics).Count().IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Summary).IsEqualTo("Error");
        await Assert.That(result.Diagnostics[0].Detail).IsEqualTo(exceptionMessage);
    }
}
