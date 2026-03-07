using System.Net;
using BeyondTrust.SecretSafeProvider;
using BeyondTrust.SecretSafeProvider.Services;

#if DEBUG
var builder = WebApplication.CreateBuilder(args);
#else
var builder = WebApplication.CreateSlimBuilder(args);
#endif

var certificate = CertificateGenerator.GenerateSelfSignedCertificate("CN=127.0.0.1", "CN=root ca");

builder.WebHost.ConfigureKestrel(x =>
    x.Listen(IPAddress.Loopback, 0, x => x.UseHttps(x =>
    {
        x.ServerCertificate = certificate;
        x.AllowAnyClientCertificate();
    })));


// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine($"1|5|tcp|{new Uri(app.Urls.First()).GetComponents(UriComponents.HostAndPort, UriFormat.UriEscaped)}|grpc|{Convert.ToBase64String(certificate.RawData)}");
});

// Configure the HTTP request pipeline.
app.MapGrpcService<Terraform5ProviderService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
