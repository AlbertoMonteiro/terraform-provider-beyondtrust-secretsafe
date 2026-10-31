using System.Net;
using System.Text;
using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Services;
using Refit;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class ApiWireFormatTests
{
    private readonly RecordingHandler _handler = new();
    private readonly IBeyondTrustSecretSafe _sut;

    public ApiWireFormatTests()
    {
        var client = new HttpClient(_handler) { BaseAddress = new Uri("https://secretsafe.example.com") };

        _sut = RestService.For<IBeyondTrustSecretSafe>(client, new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(BeyondTrust.SecretSafeProvider.Serialization.Json.Default.Options)
        });
    }

    [Test]
    public async Task CreateFolder_SendsJsonBody()
    {
        // Arrange
        _handler.Response = () => Json("""{ "Id": "id", "Name": "Folder" }""");

        // Act
        await _sut.CreateFolder(new FolderRequest(1048, "Folder", null, null, 1));

        // Assert
        await Assert.That(_handler.ContentType).IsEqualTo("application/json");
        await Assert.That(_handler.Body).Contains("\"OwnerId\":1048");
        await Assert.That(_handler.Body).Contains("\"Name\":\"Folder\"");
    }

    [Test]
    public async Task UpdateFolder_SendsJsonBody()
    {
        // Arrange
        _handler.Response = () => Json("""{ "Id": "id", "Name": "Folder" }""");

        // Act
        await _sut.UpdateFolder("id", new FolderRequest(1048, "Folder", null, null, 1));

        // Assert
        await Assert.That(_handler.ContentType).IsEqualTo("application/json");
    }

    [Test]
    public async Task UpdateCredentialSecret_WithNoContentResponse_SendsFolderId()
    {
        // Arrange
        _handler.Response = () => new HttpResponseMessage(HttpStatusCode.NoContent);

        // Act
        await _sut.UpdateCredentialSecret("id", new CreateSecretCredentialRequest("Title", null, "user", "password", 1048, FolderId: "folder"));

        // Assert
        await Assert.That(_handler.Body).Contains("\"FolderId\":\"folder\"");
    }

    [Test]
    public async Task CreateCredentialSecret_OmitsFolderId()
    {
        // Arrange
        _handler.Response = () => Json("""{ "Id": "id" }""");

        // Act
        await _sut.CreateCredentialSecret("folder", new CreateSecretCredentialRequest("Title", null, "user", "password", 1048));

        // Assert
        await Assert.That(_handler.Body).DoesNotContain("FolderId");
    }

    [Test]
    public async Task DeleteSecret_UsesSecretPath()
    {
        // Arrange
        _handler.Response = () => new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        await _sut.DeleteSecret("id");

        // Assert
        await Assert.That(_handler.Method).IsEqualTo(HttpMethod.Delete);
        await Assert.That(_handler.Path).IsEqualTo("/public/v3/Secrets-Safe/Secrets/id");
    }

    private static HttpResponseMessage Json(string body)
        => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Func<HttpResponseMessage> Response { get; set; } = () => new HttpResponseMessage(HttpStatusCode.OK);
        public string? Body { get; private set; }
        public string? ContentType { get; private set; }
        public HttpMethod? Method { get; private set; }
        public string? Path { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Path = request.RequestUri!.AbsolutePath;
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return Response();
        }
    }
}
