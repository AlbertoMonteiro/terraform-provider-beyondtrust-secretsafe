# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Purpose

A Terraform provider for BeyondTrust Secret Safe, implemented in C# (.NET 10) using Native AOT compilation. It implements the Terraform Plugin Protocol v5.10 over gRPC/mTLS.

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

### Local Testing on Windows

To test the provider with the real Terraform CLI on Windows, publish the AOT binary and point Terraform at it. This needs no Docker: the Visual Studio C++ tools do the native link.

```powershell
# The AOT link step calls vswhere.exe, which is not on the PATH by default
$env:PATH = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer;$env:PATH"

# Publishes win-x86 to dist/ and deletes terraform/.terraform and its lock file.
# It deletes dist/ first, so do not run it while Terraform is using the binary
./publish-dev.ps1

# Sets TF_CLI_CONFIG_FILE to terraform/dev.tfrc, then runs terraform init and plan on terraform/
./test-provider-local.ps1
```

- `terraform/dev.tfrc` holds the `dev_overrides` entry for `albertomonteiro/beyondtrust-secretsafe`. The key must match the `source` in `required_providers` exactly.
- Running `terraform` by hand needs `$env:TF_CLI_CONFIG_FILE` set first. Without a matching override, Terraform uses the published provider from the registry and rejects anything newer, such as a new argument, as unsupported.
- `terraform init` still downloads the published provider and warns that it can be skipped. That is expected: the override wins when `plan` runs.
- If a new provider argument is rejected, check which binary is in use: the "development overrides" warning must list `..\dist`.
- `terraform/` is in a public repository. Pass credentials with `TF_VAR_*` variables, not in `main.tf`. State files hold secrets in plain text and are ignored by `terraform/.gitignore`.

The test suite does not run under Native AOT, so anything touching serialization must also be exercised on the published binary. Failures such as MessagePack generic formatters only appear there.

The local Docker daemon is Rancher Desktop, which the integration tests and the Alpine build need. Start it with `rdctl start` (`C:\Program Files\Rancher Desktop\resources\resources\win32\bin\rdctl.exe`).

## Architecture

### Terraform Plugin Protocol v5.10 Handshake

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

- `Protos/tfplugin5.10.proto` — official HashiCorp Terraform Plugin Protocol v5.10 definition, taken from the `v1.17.1` release tag. The only local change is `option csharp_namespace`. Generates the `Provider` and `Provisioner` gRPC service stubs under namespace `BeyondTrust.SecretSafeProvider.Proto`, and the test project has its own copy. Do not modify it otherwise: to move to a newer protocol, copy the file from a newer release tag.
- `Protos/greet.proto` — template/placeholder leftover from project scaffolding; to be replaced with the actual provider service implementation.

### Current State (Work in Progress)

- `Services/GreeterService.cs` is a scaffolding placeholder. The real implementation must subclass `Provider.ProviderBase` (generated from `tfplugin5.10.proto`) and implement all RPCs: `GetSchema`, `PrepareProviderConfig`, `Configure`, `ReadResource`, `PlanResourceChange`, `ApplyResourceChange`, `ImportResourceState`, `ReadDataSource`, `ValidateResourceTypeConfig`, `ValidateDataSourceConfig`, `UpgradeResourceState`, and `Stop`.
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