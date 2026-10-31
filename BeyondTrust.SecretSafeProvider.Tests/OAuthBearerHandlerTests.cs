using System.Buffers.Text;
using System.Net;
using System.Text;
using BeyondTrust.SecretSafeProvider.Services;
using Refit;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class OAuthBearerHandlerTests
{
    private const string BASE_ADDRESS = "https://secretsafe.example.com";
    private const string CLIENT_ID = "my-client";
    private const string CLIENT_SECRET = "s3cr&t=value";
    private const string TOKEN_JSON = """{ "access_token": "my-token", "expires_in": 3600, "token_type": "Bearer", "scope": "publicapi" }""";

    private readonly RecordingHandler _inner = new();

    private HttpClient CreateClient(string baseAddress = BASE_ADDRESS)
    {
        var sut = new OAuthBearerHandler(new Uri(baseAddress), CLIENT_ID, CLIENT_SECRET, _inner);
        return new HttpClient(sut) { BaseAddress = new Uri(baseAddress) };
    }

    [Test]
    public async Task SendAsync_OnFirstRequest_RequestsTokenWithClientCredentials()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        await client.GetAsync("/public/v3/anything");

        // Assert
        var (tokenRequest, body) = _inner.Requests[0];
        await Assert.That(tokenRequest.Method).IsEqualTo(HttpMethod.Post);
        await Assert.That(tokenRequest.Content!.Headers.ContentType!.MediaType).IsEqualTo("application/x-www-form-urlencoded");
        await Assert.That(body).IsEqualTo("grant_type=client_credentials&client_id=my-client&client_secret=s3cr%26t%3Dvalue");
    }

    [Test]
    [Arguments("https://secretsafe.example.com", "https://secretsafe.example.com/public/v3/Auth/connect/token")]
    [Arguments("https://secretsafe.example.com/", "https://secretsafe.example.com/public/v3/Auth/connect/token")]
    [Arguments("https://secretsafe.example.com/BeyondTrust/api", "https://secretsafe.example.com/BeyondTrust/api/public/v3/Auth/connect/token")]
    [Arguments("https://secretsafe.example.com/BeyondTrust/api/", "https://secretsafe.example.com/BeyondTrust/api/public/v3/Auth/connect/token")]
    public async Task SendAsync_RequestsTokenRelativeToBaseAddress(string baseAddress, string expectedUrl)
    {
        // Arrange
        using var client = CreateClient(baseAddress);

        // Act
        await client.GetAsync("/public/v3/anything");

        // Assert
        await Assert.That(_inner.Requests[0].Request.RequestUri!.AbsoluteUri).IsEqualTo(expectedUrl);
    }

    [Test]
    public async Task SendAsync_AddsBearerTokenToRequest()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        await client.GetAsync("/public/v3/anything");

        // Assert
        var authorization = _inner.Requests[1].Request.Headers.Authorization;
        await Assert.That(authorization!.Scheme).IsEqualTo("Bearer");
        await Assert.That(authorization.Parameter).IsEqualTo("my-token");
    }

    [Test]
    public async Task SendAsync_WhenCalledTwice_RequestsTokenOnce()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        await client.GetAsync("/public/v3/first");
        await client.GetAsync("/public/v3/second");

        // Assert
        await Assert.That(_inner.Requests).Count().IsEqualTo(3);
        await Assert.That(_inner.Requests.Count(r => r.Request.RequestUri!.AbsolutePath.EndsWith("connect/token"))).IsEqualTo(1);
    }

    [Test]
    public async Task SendAsync_WhenTokenRequestFails_ThrowsWithoutCallingApiOrExposingSecret()
    {
        // Arrange
        _inner.TokenStatus = HttpStatusCode.Unauthorized;
        using var client = CreateClient();

        // Act
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/public/v3/anything"));

        // Assert
        await Assert.That(exception!.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        await Assert.That(exception.Message).DoesNotContain(CLIENT_SECRET);
        await Assert.That(_inner.Requests).Count().IsEqualTo(1);
    }

    [Test]
    public async Task SendAsync_WhenTokenResponseHasNoAccessToken_Throws()
    {
        // Arrange
        _inner.TokenBody = "{}";
        using var client = CreateClient();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/public/v3/anything"));

        // Assert
        await Assert.That(exception!.Message).Contains("access_token");
    }

    [Test]
    [Arguments("""{ "client_id": "my-client", "_clientUserId": "1048" }""")]
    [Arguments("""{ "client_id": "my-client", "_clientUserId": 1048 }""")]
    public async Task GetClientUserId_ReadsClaimFromToken(string claims)
    {
        // Arrange
        _inner.TokenBody = TokenResponse(Jwt(claims));
        var sut = new OAuthBearerHandler(new Uri(BASE_ADDRESS), CLIENT_ID, CLIENT_SECRET, _inner);

        // Act
        var userId = await sut.GetClientUserId();

        // Assert
        await Assert.That(userId).IsEqualTo(1048);
    }

    [Test]
    public async Task GetClientUserId_WhenCalledTwice_RequestsTokenOnce()
    {
        // Arrange
        _inner.TokenBody = TokenResponse(Jwt("""{ "_clientUserId": "1048" }"""));
        var sut = new OAuthBearerHandler(new Uri(BASE_ADDRESS), CLIENT_ID, CLIENT_SECRET, _inner);

        // Act
        await sut.GetClientUserId();
        await sut.GetClientUserId();

        // Assert
        await Assert.That(_inner.Requests).Count().IsEqualTo(1);
    }

    [Test]
    [Arguments("opaque-token")]
    [Arguments("header.%%%.signature")]
    [Arguments("header.bm90LWpzb24.signature")]
    public async Task GetClientUserId_WhenTokenIsNotAReadableJwt_ThrowsAskingForOwnerId(string accessToken)
    {
        // Arrange
        _inner.TokenBody = TokenResponse(accessToken);
        var sut = new OAuthBearerHandler(new Uri(BASE_ADDRESS), CLIENT_ID, CLIENT_SECRET, _inner);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetClientUserId());

        // Assert
        await Assert.That(exception!.Message).Contains("owner_id");
    }

    [Test]
    public async Task GetClientUserId_WhenClaimIsMissing_ThrowsAskingForOwnerId()
    {
        // Arrange
        _inner.TokenBody = TokenResponse(Jwt("""{ "client_id": "my-client" }"""));
        var sut = new OAuthBearerHandler(new Uri(BASE_ADDRESS), CLIENT_ID, CLIENT_SECRET, _inner);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetClientUserId());

        // Assert
        await Assert.That(exception!.Message).Contains("owner_id");
    }

    private static string Jwt(string claims)
        => $"header.{Base64Url.EncodeToString(Encoding.UTF8.GetBytes(claims))}.signature";

    private static string TokenResponse(string accessToken)
        => $$"""{ "access_token": "{{accessToken}}" }""";

    [Test]
    public async Task SendAsync_ThroughRefitClient_AuthenticatesWithBearerOnly()
    {
        // Arrange
        const string baseAddress = "https://secretsafe.example.com/BeyondTrust/api";
        using var client = CreateClient(baseAddress);
        var api = RestService.For<IBeyondTrustSecretSafe>(client, new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(BeyondTrust.SecretSafeProvider.Serialization.Json.Default.Options)
        });
        var secretId = Guid.NewGuid();

        // Act
        await api.DownloadSecret(secretId);

        // Assert
        var request = _inner.Requests[1].Request;
        await Assert.That(request.RequestUri!.AbsoluteUri)
            .IsEqualTo($"{baseAddress}/public/v3/Secrets-Safe/Secrets/{secretId}/file/download");
        await Assert.That(request.Headers.Authorization!.Scheme).IsEqualTo("Bearer");
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = [];
        public HttpStatusCode TokenStatus { get; set; } = HttpStatusCode.OK;
        public string TokenBody { get; set; } = TOKEN_JSON;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request, body));

            return request.RequestUri!.AbsolutePath.EndsWith("connect/token")
                ? new HttpResponseMessage(TokenStatus) { Content = new StringContent(TokenBody, Encoding.UTF8, "application/json") }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") };
        }
    }
}
