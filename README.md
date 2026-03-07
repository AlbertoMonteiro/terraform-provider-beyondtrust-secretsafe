# BeyondTrust Secret Safe — Terraform Provider

A Terraform provider for [BeyondTrust Secret Safe](https://www.beyondtrust.com/secrets-safe), implemented in **C# (.NET 10)** using **Native AOT** compilation and the **Terraform Plugin Protocol v5.2** over gRPC/mTLS.

## Why C#?

Most Terraform providers are written in Go because Go's stdlib includes a pure-Go TLS/crypto stack, making it trivial to ship a self-contained binary. This provider achieves the same result in C#:

- **No extra OpenSSL install required** — TLS certificate generation uses .NET's built-in `System.Security.Cryptography` (`RSA.Create`, `CertificateRequest`, `X509Certificate2`). The `hashicorp/terraform` Alpine image already ships `libssl.so.3` (via `ssl_client`), which covers the `HttpClient` HTTPS calls to the BeyondTrust API.
- **Single native binary** — AOT compilation produces a standalone executable with no .NET runtime requirement.
- **~15 MB** on Alpine/musl (`linux-musl-x64`, `TrimMode=full`).

---

## Architecture

### Terraform Plugin Protocol v5.2 Handshake

When Terraform spawns the provider binary, it reads a single handshake line from stdout before doing anything else:

```
1|5|tcp|{host}:{port}|grpc|{base64_der_cert}
```

| Field | Value |
|---|---|
| Core protocol version | `1` |
| Plugin protocol version | `5` |
| Network | `tcp` |
| Address | `127.0.0.1:{random_port}` |
| Protocol | `grpc` |
| TLS cert | DER bytes, base64 **without padding** (`=`) |

> go-plugin uses `base64.RawStdEncoding` (no padding). Trailing `=` characters cause `"illegal base64 data at input byte N"`. The handshake line must be written to raw stdout (not `Console.WriteLine`) to avoid CR/LF translation on Windows.

### gRPC Server

- Kestrel listens on `IPAddress.Loopback` port `0` (OS assigns a random port).
- HTTPS/mTLS with a self-signed certificate generated at startup via `System.Security.Cryptography`.
- `AllowAnyClientCertificate()` — Terraform presents a client cert for mutual TLS.
- `WebApplication.CreateSlimBuilder` is used (required for AOT compatibility).

### Data Source Dispatch — Strategy Pattern

`Terraform5ProviderService` has no knowledge of individual data sources. Each data source is an independent class implementing `IDataSourceHandler`, registered in DI as `IEnumerable<IDataSourceHandler>`. The service builds a lookup dictionary at startup and dispatches `ReadDataSource` calls by `TypeName`:

```
Services/
├── DataSources/
│   ├── IDataSourceHandler.cs              ← TypeName + GetSchema() + ReadAsync()
│   └── CredentialDataSourceHandler.cs     ← secretsafe_credential_data
└── Terraform5ProviderService.cs           ← orchestrates, no data source logic
```

To add a new data source: implement `IDataSourceHandler` and register it with `AddSingleton<IDataSourceHandler, NewHandler>()`. Nothing else changes.

### BeyondTrust API Client

`IBeyondTrustSecretSafe` is a [Refit](https://github.com/reactiveui/refit) interface that maps to the Secret Safe REST API. `IBeyondTrustApiFactory` creates an `HttpClient` per request using the provider configuration received from Terraform.

```
Services/
├── IBeyondTrustSecretSafe.cs   ← Refit interface: SignAppin, GetSecret, Signout, DownloadSecret
├── IBeyondTrustApiFactory.cs   ← creates IBeyondTrustSecretSafe with current config
└── BeyondTrustApiFactory.cs
```

### Serialization

Terraform communicates state via msgpack or JSON depending on context. `SmartSerializer` transparently handles both:

```
Serialization/
├── SmartSerializer.cs   ← Deserialize<T>(DynamicValue) and Serialize<T>(T) → DynamicValue
└── Json.cs              ← AOT-safe JsonSerializerContext (source generation)
```

Models use `[MessagePackObject]` + `[Key("attr_name")]` so attribute names match Terraform's snake_case convention in both msgpack and JSON.

### Project Structure

```
BeyondTrust.SecretSafeProvider/
├── Program.cs                        # Kestrel setup + handshake emission
├── CertificateGenerator.cs           # Self-signed TLS cert via System.Security.Cryptography
├── Models/
│   ├── CredentialData.cs             # secretsafe_credential_data state + schema
│   ├── ProviderConfiguration.cs      # Provider block attributes (key, runas, baseUrl)
│   ├── SecretValue.cs                # BeyondTrust API response model
│   ├── KeyAndRunAs.cs                # PS-Auth header value
│   └── TfTypes.cs                    # Terraform type byte strings
├── Serialization/
│   ├── SmartSerializer.cs            # msgpack/JSON unified serializer
│   └── Json.cs                       # Source-generated JsonSerializerContext
├── Services/
│   ├── IBeyondTrustSecretSafe.cs     # Refit HTTP client interface
│   ├── IBeyondTrustApiFactory.cs
│   ├── BeyondTrustApiFactory.cs
│   ├── DataSources/
│   │   ├── IDataSourceHandler.cs
│   │   └── CredentialDataSourceHandler.cs
│   └── Terraform5ProviderService.cs  # gRPC Provider.ProviderBase implementation
└── Protos/
    └── tfplugin5.2.proto             # Official Terraform Plugin Protocol v5.2

BeyondTrust.SecretSafeProvider.AppHost/
├── AppHost.cs                        # .NET Aspire host (WireMock + provider)
└── __admin/mappings/                 # WireMock mock definitions
    ├── auth-signappin.json
    ├── auth-signout.json
    ├── secrets-get.json
    └── secrets-download.json

terraform/
└── main.tf                           # Local dev Terraform config

Dockerfile.test                       # Multistage: AOT build + terraform plan
publish-dev.ps1                       # Windows dev publish script
```

---

## Prerequisites

### Windows development

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Terraform CLI](https://developer.hashicorp.com/terraform/install)
- [.NET Aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling) (for local API mocking)

### Linux/Alpine builds (AOT)

- Docker (the build runs inside a container — no local toolchain needed)

---

## Building

### Windows (debug / local dev)

```powershell
# Restore and build
dotnet build BeyondTrust.SecretSafeProvider/BeyondTrust.SecretSafeProvider.csproj

# Publish a self-contained win-x64 binary to dist/ (removes everything except the .exe)
.\publish-dev.ps1
```

`publish-dev.ps1` runs `dotnet publish -r win-x64 -o dist` and removes all files except the `.exe`, ensuring Terraform's directory scan picks the correct file.

### Linux/Alpine (Native AOT — production)

```bash
docker build -f Dockerfile.test -t bt-provider-test .
```

The `Dockerfile.test` is a multistage build:

1. **Stage 1** (`mcr.microsoft.com/dotnet/sdk:10.0-alpine`) — installs the native toolchain (`clang`, `gcc`, `build-base`, `zlib-dev`, `musl-dev`, `gcompat`) and publishes the AOT binary.
2. **Stage 2** (`hashicorp/terraform:latest`) — copies the binary, configures `dev_overrides`, and runs `terraform plan` against the WireMock server.

> `gcompat` is required in the build stage so that `Grpc.Tools`' glibc-compiled `protoc` binary can run on musl Alpine via a glibc compatibility layer.

**Key publish flags:**

```bash
dotnet publish -c Release -r linux-musl-x64 -p:StaticExecutable=false -o /publish
```

`StaticExecutable=false` is intentional: a fully static musl binary's `dlopen` may not search `/usr/lib`, which could break dynamic library lookups at runtime.

---

## Local Development with Aspire

The `BeyondTrust.SecretSafeProvider.AppHost` project orchestrates local development using [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/):

- **WireMock** simulates the BeyondTrust Secret Safe API on a random port. Mappings live in `__admin/mappings/` and are hot-reloaded on change.
- The provider binary is configured via Aspire service discovery to point at the WireMock instance.

```bash
dotnet run --project BeyondTrust.SecretSafeProvider.AppHost
```

Then in another terminal:

```bash
terraform -chdir=terraform plan
```

### Local `terraform.rc`

Add to `%APPDATA%\terraform.rc` (Windows) or `~/.terraformrc` (Linux/macOS):

```hcl
provider_installation {
  dev_overrides {
    "beyondtrust/secretsafe" = "C:/dev/beyond-trust-secret-safe-provider/dist"
  }
  direct {}
}
```

> **Provider local name**: The Terraform config uses `secretsafe` as the provider local name (matching the `secretsafe_` prefix of all data sources). The registry source address remains `beyondtrust/secretsafe`.

---

## Build Configuration

Key `.csproj` settings:

| Property | Value | Reason |
|---|---|---|
| `PublishAot` | `true` | Native AOT compilation |
| `PublishTrimmed` | `true` | Remove unused code |
| `TrimMode` | `full` | ~15 MB vs ~22 MB with `partial` |
| `StaticExecutable` | `true` | Default; overridden to `false` for Linux at publish time |
| `InvariantGlobalization` | `true` | Smaller binary; no ICU data needed |
| `StackTraceSupport` | `false` | Size optimization |
| `OptimizationPreference` | `Size` | Prefer smaller binary over speed |
| `AssemblyName` | `terraform-provider-secretsafe` | Terraform provider naming convention |

---

## Current State

### Implemented RPCs

| RPC | Status |
|---|---|
| `GetSchema` | Implemented — discovers schemas from all registered `IDataSourceHandler`s |
| `PrepareProviderConfig` | Stub (pass-through) |
| `Configure` | Implemented — deserializes `key`, `runas`, `baseUrl` from Terraform config |
| `ValidateDataSourceConfig` | Stub |
| `ValidateResourceTypeConfig` | Stub |
| `ReadDataSource` | Implemented — dispatches to handler by `TypeName`, returns Terraform Diagnostics on error |
| `ReadResource` | Stub (pass-through) |
| `PlanResourceChange` | Stub (pass-through) |
| `ApplyResourceChange` | Stub (pass-through) |
| `ImportResourceState` | Stub |
| `UpgradeResourceState` | Stub |
| `Stop` | Stub |

### Implemented Data Sources

| Data Source | Description |
|---|---|
| `secretsafe_credential_data` | Retrieves `username` and `password` from a Secret Safe secret by ID. Performs `SignAppin` → `GetSecret` → `Signout` on each read. |

### Example Usage

```hcl
terraform {
  required_providers {
    secretsafe = {
      source = "beyondtrust/secretsafe"
    }
  }
}

provider "secretsafe" {
  key     = "your-api-key"
  runas   = "service-account"
  baseUrl = "https://your-beyondtrust-instance"
}

data "secretsafe_credential_data" "db_password" {
  secret_id = "2e22e1b1-d5c2-4a17-bc90-1234567890ab"
}

output "db_username" {
  value = data.secretsafe_credential_data.db_password.username
}

output "db_password" {
  value     = data.secretsafe_credential_data.db_password.password
  sensitive = true
}
```

---

## Releasing to the Terraform Registry

Provider binary naming convention: `terraform-provider-secretsafe_{version}_{os}_{arch}`

Release checklist:

1. Build for each target OS/arch
2. Zip each binary individually
3. Generate `SHA256SUMS` file
4. GPG-sign `SHA256SUMS` with a key registered in the Terraform Registry
5. Publish the GitHub release with all artifacts

```bash
zip terraform-provider-secretsafe_1.0.0_linux_amd64.zip terraform-provider-secretsafe
sha256sum *.zip > terraform-provider-secretsafe_1.0.0_SHA256SUMS
gpg --detach-sign terraform-provider-secretsafe_1.0.0_SHA256SUMS
```

---

## Key Design Decisions

**Strategy pattern for data sources** — `Terraform5ProviderService` has zero knowledge of individual data sources. Each handler is a self-contained class registered via DI. `GetSchema` and `ReadDataSource` discover handlers at runtime through `IEnumerable<IDataSourceHandler>`, making it trivial to add new data sources without touching the service.

**Pure .NET TLS cert generation** — `CertificateGenerator` uses only `System.Security.Cryptography` BCL types (`RSA.Create`, `CertificateRequest`, `X509CertificateLoader`). On Linux, `RSA.Create()` delegates to `RSAOpenSsl`, which calls `dlopen("libssl.so.3")` — available in the `hashicorp/terraform` Alpine image via the `ssl_client` package. No third-party crypto library is required.

**SmartSerializer** — Terraform sends `DynamicValue` with either `msgpack` or `json` populated depending on context. `SmartSerializer` checks both fields and picks the non-empty one, so handlers never need to worry about which encoding is in use.

**`WebApplication.CreateSlimBuilder`** — Required for AOT compatibility. The full `WebApplication.CreateBuilder` pulls in reflection-heavy components that cannot be trimmed.

**Raw stdout write for handshake** — `Console.WriteLine` goes through a `TextWriter` that translates `\n` to `\r\n` on Windows. go-plugin's `bufio.Scanner` strips `\n` but leaves `\r`, corrupting the base64 cert. Writing directly to `Console.OpenStandardOutput()` bypasses this.

**Base64 no-padding** — go-plugin uses `base64.RawStdEncoding`. The `.TrimEnd('=')` call on the cert string is mandatory.

**Terraform Diagnostics instead of exceptions** — `ReadDataSource` catches all handler exceptions and returns them as `Diagnostic` messages with `Summary` and `Detail` fields. This gives Terraform users actionable error messages instead of raw gRPC status codes.
