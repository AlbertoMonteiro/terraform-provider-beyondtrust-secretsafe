# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Purpose

A Terraform provider for BeyondTrust Secret Safe, implemented in C# (.NET 10) using Native AOT compilation. It implements the Terraform Plugin Protocol v5.2 over gRPC/mTLS.

## Build Commands

```bash
# Restore dependencies
dotnet restore BeyondTrust.SecretSafeProvider/BeyondTrust.SecretSafeProvider.csproj

# Build (debug)
dotnet build BeyondTrust.SecretSafeProvider/BeyondTrust.SecretSafeProvider.csproj

# Run locally (debug mode uses full WebApplication builder)
dotnet run --project BeyondTrust.SecretSafeProvider/BeyondTrust.SecretSafeProvider.csproj

# Publish as native AOT binary for Linux/Alpine (must run inside Alpine Docker)
dotnet publish -c Release -r linux-musl-x64 -o /app/publish --self-contained true
```

The build properties (`PublishAot`, `StaticExecutable=true`, `StaticOpenSslLinking=true`,
`InvariantGlobalization=true`) live in the `.csproj`, producing a fully static binary with
OpenSSL linked in. See `DESIGN_DECISIONS.md` for the rationale.

For Alpine-targeted builds, use a Docker container with musl toolchain:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
RUN apk add --no-cache clang gcc build-base zlib-dev musl-dev
```

## Architecture

### Terraform Plugin Protocol v5.2 Handshake

When Terraform spawns the provider binary, it reads a single line from stdout before doing anything else. `Program.cs` emits this on `ApplicationStarted`:

```
1|5|tcp|{host}:{port}|grpc|{base64_der_cert}
```

Fields: `core_protocol_version|plugin_protocol_version|network|address|protocol|tls_cert_base64`

Nothing else may be written to stdout before this line.

### gRPC Server

- Kestrel listens on `IPAddress.Loopback` port `0` (random) with HTTPS/mTLS
- `CertificateGenerator.GenerateSelfSignedCertificate` produces the TLS cert; its DER bytes (base64) are embedded in the handshake string
- `AllowAnyClientCertificate()` is set — Terraform presents a client cert for mutual TLS
- In `DEBUG`, uses `WebApplication.CreateBuilder`; in `Release`, uses `WebApplication.CreateSlimBuilder` (required for AOT compatibility)

### Proto Files

- `Protos/tfplugin5.2.proto` — official HashiCorp Terraform Plugin Protocol v5.2 definition (do not modify). Generates the `Provider` and `Provisioner` gRPC service stubs under namespace `CSF.Terraform.SecretSafe`.
- `Protos/greet.proto` — template/placeholder leftover from project scaffolding; to be replaced with the actual provider service implementation.

### Current State (Work in Progress)

- `Services/GreeterService.cs` is a scaffolding placeholder. The real implementation must subclass `Provider.ProviderBase` (generated from `tfplugin5.2.proto`) and implement all RPCs: `GetSchema`, `PrepareProviderConfig`, `Configure`, `ReadResource`, `PlanResourceChange`, `ApplyResourceChange`, `ImportResourceState`, `ReadDataSource`, `ValidateResourceTypeConfig`, `ValidateDataSourceConfig`, `UpgradeResourceState`, and `Stop`.
- `CertificateGenerator.cs` contains placeholder base64 content — it must be replaced with actual PKCS#12 certificate generation logic before the provider can function.

### AOT Constraints

- `PublishTrimmed=true` with `TrimMode=full` — avoid reflection-based patterns; use source generators
- `StackTraceSupport=false` and `OptimizationPreference=Size` — binary size optimized
- `StaticExecutable=true` + `StaticOpenSslLinking=true` — fully static binary with OpenSSL linked in (requires musl + `openssl-libs-static` on the build host)
- `InvariantGlobalization=true` — disables ICU; if turned off, also enable `StaticICULinking` to keep the binary self-contained
- gRPC protobuf source generation happens at build time via `<Protobuf>` items in the `.csproj`

### Terraform Registry Packaging

Provider binary naming convention: `terraform-provider-{name}_{version}_{os}_{arch}`

Release artifacts require:
1. Zip the binary per OS/arch
2. Generate `SHA256SUMS` file
3. GPG-sign the `SHA256SUMS` file with a key registered in the Terraform Registry

### Testing

- **IMPORTANT: ALL test executions MUST use the following command:**
  ```bash
  dotnet run --project BeyondTrust.SecretSafeProvider.Tests/BeyondTrust.SecretSafeProvider.Tests.csproj --disable-logo --fail-fast --no-progress --no-ansi
  ```
  Do NOT use `dotnet test` — it is not supported by Microsoft.Testing.Platform on .NET 10 SDK.
- For testing framework, should use TUnit: https://github.com/thomhurst/TUnit
- For mock, should use Imposter: https://github.com/themidnightgospel/Imposter
    - For more Imposter docs check: https://themidnightgospel.github.io/Imposter/latest/methods/throwing/
- Tests should use trible AAA pattern, Arrange, Act, Assert
- Test should use the System Under Test pattern, naming the class that is being tested as _sut