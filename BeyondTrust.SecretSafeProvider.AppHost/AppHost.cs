var builder = DistributedApplication.CreateBuilder(args);

var repoRoot = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, ".."));
var mappingsPath = Path.Combine(Directory.GetCurrentDirectory(), "__admin", "mappings");

var secretsafe = builder
    .AddWireMock("secretsafe-mock")
    .AsHttp2Service()
    .WithMappingsPath(mappingsPath)
    .WithWatchStaticMappings()
    .WithOpenTelemetry()
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddDockerfile("terraform", repoRoot, Path.Combine(repoRoot, "Dockerfile.test"))
    .WaitFor(secretsafe)
    .WithReference(secretsafe);

//builder.AddProject<Projects.BeyondTrust_SecretSafeProvider>("provider")
//    .WithHttpsEndpoint(port: 9999, targetPort: 5000, name: "dashboard")
//    .WaitFor(secretsafe)
//    .WithReference(secretsafe);

builder.Build().Run();
