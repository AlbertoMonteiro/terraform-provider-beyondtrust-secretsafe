var builder = DistributedApplication.CreateBuilder(args);

var repoRoot = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, ".."));
var mappingsPath = Path.Combine(builder.AppHostDirectory, "__admin", "mappings");

var secretsafe = builder
    .AddWireMock("secretsafe-mock")
    .AsHttp2Service()
    .WithMappingsPath(mappingsPath)
    .WithWatchStaticMappings()
    .WithOpenTelemetry()
    .WithLifetime(ContainerLifetime.Persistent);

//builder.AddDockerfile("bt-provider-test", repoRoot, Path.Combine(repoRoot, "Dockerfile.test"))
//    .WithImageTag("latest")
//    .WaitFor(secretsafe)
//    .WithReference(secretsafe)
//    .WithEnvironment("BEYONDTRUST_URL", secretsafe.Resource.GetEndpoint("http")); ;

builder.AddProject<Projects.BeyondTrust_SecretSafeProvider>("provider")
    .WithHttpsEndpoint(port: 9999, targetPort: 5000, name: "direct")
    .WaitFor(secretsafe)
    .WithReference(secretsafe)
    .WithEnvironment("BEYONDTRUST_URL", secretsafe.Resource.GetEndpoint("http"));

builder.Build().Run();
