using Aspire.Hosting;
using Aspire.Hosting.Testing;
using BeyondTrust.SecretSafeProvider.Tests.Proto;
using Grpc.Net.Client;
using System.Net;
using System.Net.Security;
using TUnit.Core.Interfaces;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class AspireSetup : IAsyncInitializer, IAsyncDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    public DistributedApplication App { get; private set; }
    public HttpClient GrpcHttpClient {get;private set;}
    public Provider.ProviderClient Client {get;private set;}

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.BeyondTrust_SecretSafeProvider>();

        App = await appHost.BuildAsync()
            .WaitAsync(DefaultTimeout);

        await App.StartAsync()
            .WaitAsync(DefaultTimeout);

        using var httpClient = App.CreateHttpClient("provider", "direct");

        GrpcHttpClient = BuildGrpcHttpClient(httpClient.BaseAddress!);

        var channel = GrpcChannel.ForAddress(GrpcHttpClient.BaseAddress!, new GrpcChannelOptions
        {
            HttpClient = GrpcHttpClient
        });

        Client = new Provider.ProviderClient(channel);
    }

    public async ValueTask DisposeAsync()
    {
        GrpcHttpClient.Dispose();
        await App.DisposeAsync();
    }

    private static HttpClient BuildGrpcHttpClient(Uri baseAddress)
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

        return new HttpClient(httpHandler) { BaseAddress = baseAddress };
    }
}
