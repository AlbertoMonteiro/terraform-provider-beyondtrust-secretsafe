# BeyondTrust Secret Safe — Terraform Provider

A Terraform provider for [BeyondTrust Secret Safe](https://www.beyondtrust.com/secrets-safe), implemented in **C# (.NET 10)** using **Native AOT** compilation and the **Terraform Plugin Protocol v5.2** over gRPC/mTLS.

## Why C#?

Most Terraform providers are written in Go because Go's stdlib includes a pure-Go TLS/crypto stack, making it trivial to ship a self-contained binary. This provider demonstrates that C# + Native AOT can achieve the same result:

- **No OpenSSL dependency at runtime** — TLS certificate generation uses [BouncyCastle.Cryptography](https://www.bouncycastle.org/csharp/), a pure .NET crypto library, avoiding the Linux `dlopen("libssl.so.3")` call that `RSA.Create()` would otherwise make.
- **Single native binary** — AOT compilation produces a standalone executable with no .NET runtime requirement.
- **~15 MB** on Alpine/musl (linux-musl-x64, TrimMode=full).

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
- HTTPS/mTLS with a self-signed certificate generated at startup via BouncyCastle — no OpenSSL required.
- `AllowAnyClientCertificate()` — Terraform presents a client cert for mutual TLS.
- `WebApplication.CreateSlimBuilder` is used (required for AOT compatibility).

### Certificate Generation

`CertificateGenerator` uses BouncyCastle to generate an RSA 2048 keypair and a self-signed X.509 certificate entirely in managed code. The result is exported to PKCS12, loaded into `X509Certificate2`, and passed to Kestrel. No OpenSSL is touched at any point.

### Project Structure

```
BeyondTrust.SecretSafeProvider/
├── Program.cs                        # Kestrel setup + handshake emission
├── CertificateGenerator.cs           # Pure-managed TLS cert via BouncyCastle
├── Services/
│   └── Terraform5ProviderService.cs  # gRPC service implementing Provider.ProviderBase
└── Protos/
    └── tfplugin5.2.proto             # Official HashiCorp Terraform Plugin Protocol v5.2

terraform/
└── main.tf                           # Example Terraform config for local testing

Dockerfile.test                       # Multistage: AOT build + terraform plan test
publish-dev.ps1                       # Windows dev publish script
```

---

## Prerequisites

### For Windows development

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Terraform CLI](https://developer.hashicorp.com/terraform/install) (for local testing)

### For Linux/Alpine builds (AOT)

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

The `publish-dev.ps1` script runs `dotnet publish -r win-x64 -o dist` and removes all files except the `.exe`, ensuring Terraform's directory scan picks the correct file.

### Linux/Alpine (Native AOT — production)

Build inside Docker using the musl toolchain:

```bash
docker build -f Dockerfile.test -t bt-provider-test .
```

The `Dockerfile.test` is a multistage build:

1. **Stage 1** (`mcr.microsoft.com/dotnet/sdk:10.0-alpine`) — installs the native toolchain (`clang`, `gcc`, `build-base`, `zlib-dev`, `musl-dev`, `gcompat`) and publishes the AOT binary.
2. **Stage 2** (`hashicorp/terraform:latest`) — copies the binary, configures `dev_overrides`, and runs `terraform plan`.

> `gcompat` is required in the build stage so that `Grpc.Tools`' glibc-compiled `protoc` binary can run on musl Alpine via a glibc compatibility layer.

**Key publish flags:**

```bash
dotnet publish -c Release -r linux-musl-x64 -p:StaticExecutable=false -o /publish
```

`StaticExecutable=false` is intentional: a fully static musl binary's `dlopen` may not search `/usr/lib`, which could break any dynamic library lookups. Dynamic linking (musl dynamic linker) is used instead.

---

## Testing

### Docker (recommended)

```bash
# Build and run terraform plan end-to-end
docker build -f Dockerfile.test -t bt-provider-test .
docker run --rm bt-provider-test
```

Expected output:

```
data.beyondtrust_hello.example: Read complete after 0s

Changes to Outputs:
  + content = "data from .NET 10"
```

### Local (Windows)

1. Run `.\publish-dev.ps1` to produce `dist\terraform-provider-secretsafe.exe`.
2. Add a `dev_overrides` block to `%APPDATA%\terraform.rc`:

```hcl
provider_installation {
  dev_overrides {
    "beyondtrust/secretsafe" = "C:/dev/beyond-trust-secret-safe-provider/dist"
  }
  direct {}
}
```

3. Run `terraform plan` from the `terraform/` directory.

---

## Build Configuration

Key `.csproj` settings:

| Property | Value | Reason |
|---|---|---|
| `PublishAot` | `true` | Native AOT compilation |
| `PublishTrimmed` | `true` | Remove unused code |
| `TrimMode` | `full` | Aggressive trim (~15 MB vs ~22 MB with `partial`) |
| `StaticExecutable` | `true` | Default; overridden to `false` at publish time for Linux |
| `InvariantGlobalization` | `true` | Smaller binary; no ICU data needed |
| `StackTraceSupport` | `false` | Size optimization |
| `OptimizationPreference` | `Size` | Prefer smaller binary over speed |
| `AssemblyName` | `terraform-provider-secretsafe` | Terraform provider naming convention |

---

## Current State

The provider skeleton is complete and end-to-end functional:

| RPC | Status |
|---|---|
| `GetSchema` | Implemented — exposes `beyondtrust_hello` data source |
| `PrepareProviderConfig` | Stub (pass-through) |
| `Configure` | Stub |
| `ValidateDataSourceConfig` | Stub |
| `ValidateResourceTypeConfig` | Stub |
| `ReadDataSource` | Implemented — returns `"data from .NET 10"` |
| `ReadResource` | Stub (pass-through) |
| `PlanResourceChange` | Stub (pass-through) |
| `ApplyResourceChange` | Stub (pass-through) |
| `ImportResourceState` | Stub |
| `UpgradeResourceState` | Stub |
| `Stop` | Stub |

The next step is to implement the actual BeyondTrust Secret Safe API calls inside `Configure` (to authenticate) and `ReadDataSource` (to retrieve secrets).

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
# Example for linux_amd64
zip terraform-provider-secretsafe_1.0.0_linux_amd64.zip terraform-provider-secretsafe
sha256sum *.zip > terraform-provider-secretsafe_1.0.0_SHA256SUMS
gpg --detach-sign terraform-provider-secretsafe_1.0.0_SHA256SUMS
```

---

## Key Design Decisions

**BouncyCastle for TLS cert generation** — On Linux, `RSA.Create()` returns `RSAOpenSsl`, which calls `dlopen("libssl.so.3")`. Since `libssl` is not present in the stock `hashicorp/terraform` Alpine image, using the .NET stdlib crypto would require users to install OpenSSL. BouncyCastle eliminates this dependency entirely, making the binary self-contained on any OS.

**`WebApplication.CreateSlimBuilder`** — Required for AOT compatibility. The full `WebApplication.CreateBuilder` pulls in reflection-heavy components that cannot be trimmed.

**Raw stdout write for handshake** — `Console.WriteLine` goes through a `TextWriter` that may translate `\n` to `\r\n` on Windows. go-plugin's `bufio.Scanner` strips `\n` but leaves `\r`, which would corrupt the base64 cert. Writing directly to `Console.OpenStandardOutput()` bypasses this.

**Base64 no-padding** — go-plugin uses `base64.RawStdEncoding`. The `.TrimEnd('=')` call on the cert string is mandatory.
