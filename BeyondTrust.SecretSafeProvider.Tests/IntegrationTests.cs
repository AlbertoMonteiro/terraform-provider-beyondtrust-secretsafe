using Aspire.Hosting.Testing;
using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Services;
using BeyondTrust.SecretSafeProvider.Tests.Proto;
using Google.Protobuf;
using Grpc.Net.Client;
using MessagePack;
using System.Net;
using System.Net.Security;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class IntegrationTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    [Test]
    public async Task ReadDataSource_WithCredentialDataRequest_ReturnsSecretFromProvider()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        var appHost = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.BeyondTrust_SecretSafeProvider>();

        await using var app = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        await app.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        CredentialData credential = new() { SecretId = Guid.NewGuid().ToString() };

        ReadDataSource.Types.Request request = new()
        {
            TypeName = CredentialDataSourceHandler.TYPE_NAME,
            Config = new()
            {
                Json = ByteString.Empty,
                Msgpack = ByteString.CopyFrom(MessagePackSerializer.Serialize(credential)),
            }
        };

        // Act
        using var httpClient = app.CreateHttpClient("provider", "direct");
        using var grpcReadyHttpClient = BuildGrpcHttpClient(httpClient);

        var channel = GrpcChannel.ForAddress(grpcReadyHttpClient.BaseAddress, new GrpcChannelOptions
        {
            HttpClient = grpcReadyHttpClient
        });

        var client = new Provider.ProviderClient(channel);

        var resp = await client.ReadDataSourceAsync(request);
        var responseData = MessagePackSerializer.Deserialize<CredentialData>(resp.State.Msgpack.Memory);

        // Assert

        await Assert.That(responseData.Username).IsEqualTo("service-account");
        await Assert.That(responseData.Password).IsEqualTo("SuperSecret123!");
        await Assert.That(responseData.SecretId).IsEqualTo(credential.SecretId);
    }

    private static HttpClient BuildGrpcHttpClient(HttpClient httpClient)
    {
        var httpHandler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls13,
                ApplicationProtocols = [SslApplicationProtocol.Http3],
                RemoteCertificateValidationCallback = (_, _, _, _) => true
            },
            EnableMultipleHttp2Connections = true,
            AutomaticDecompression = DecompressionMethods.All,
            MaxConnectionsPerServer = 10
        };

        var grpcReadyHttpClient = new HttpClient(httpHandler)
        {
            BaseAddress = httpClient.BaseAddress
        };
        return grpcReadyHttpClient;
    }
}
