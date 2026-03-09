using System.Net;
using BeyondTrust.SecretSafeProvider;
using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Services;
using BeyondTrust.SecretSafeProvider.Services.DataSources;
using BeyondTrust.SecretSafeProvider.Services.Resources;
using Refit;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Logging.ClearProviders();

var certificate = CertificateGenerator.GenerateSelfSignedCertificate("CN=127.0.0.1", "CN=root ca");

var port = builder.Configuration.GetValue<int?>("SERVICE_PORT") ?? 0;

builder.WebHost.ConfigureKestrel(x =>
    x.Listen(IPAddress.Loopback, port, x => x.UseHttps(x =>
    {
        x.ServerCertificate = certificate;

        x.AllowAnyClientCertificate();
    })));

builder.Services.AddGrpc();

var emptyConfiguration = new ProviderConfiguration()
{
    BaseUrl = "",
    Key = "",
    RunAs = ""
};
builder.Services.AddSingleton<IBeyondTrustApiFactory, BeyondTrustApiFactory>();
builder.Services.AddSingleton(emptyConfiguration);
builder.Services.AddSingleton<IDataSourceHandler, CredentialDataSourceHandler>();
builder.Services.AddSingleton<IDataSourceHandler, FileDownloadDataSourceHandler>();
builder.Services.AddSingleton<IResourceHandler, FolderCredentialResourceHandler>();
builder.Services.AddSingleton<IResourceHandler, FolderFileSecretResourceHandler>();

var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() =>
{
    // Write directly to the raw stdout stream to avoid any TextWriter CR/LF translation.
    // go-plugin's bufio.Scanner strips \n but leaves \r, which would corrupt the base64 cert.
    var address = new Uri(app.Urls.First()).GetComponents(UriComponents.HostAndPort, UriFormat.UriEscaped);
    // go-plugin uses base64.RawStdEncoding (no padding), so strip trailing '=' chars.
    var cert = Convert.ToBase64String(certificate.RawData).Replace("\r", "").Replace("\n", "").TrimEnd('=');
    var line = $"1|5|tcp|{address}|grpc|{cert}\n";
    var bytes = System.Text.Encoding.ASCII.GetBytes(line);

    using var stdout = Console.OpenStandardOutput();
    stdout.Write(bytes);
    stdout.Flush();
});

app.MapGrpcService<Terraform5ProviderService>();

app.Run();
