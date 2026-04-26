# BeyondTrust Secret Safe Terraform Provider

A Terraform provider for [BeyondTrust Secret Safe](https://www.beyondtrust.com/secrets-safe), implemented in **C# (.NET 10)** with **Native AOT** compilation for minimal resource footprint.

## Features

- 🔐 **Retrieve & Manage Secrets** - Query credentials and file secrets from BeyondTrust Secret Safe
- 📦 **Native AOT Compiled** - Minimal binary size (~15 MB) with no runtime dependencies
- 🔄 **Full TPPv5.2 Support** - Implements Terraform Plugin Protocol v5.2 over gRPC
- 🛡️ **mTLS Security** - Self-signed certificates for encrypted provider-Terraform communication
- 📁 **Folder Management** - Create, read, and manage folders in Secret Safe
- ⚡ **Fast & Lightweight** - Optimized for performance and quick startup

## Quick Start

### Installation

```hcl
terraform {
  required_providers {
    secretsafe = {
      source  = "albertomonteiro/beyondtrust-secretsafe"
      version = "~> 0.0"
    }
  }
}
```

### Configuration

```hcl
provider "secretsafe" {
  runas    = "terraform-user"
  key      = var.secret_safe_api_key
  baseUrl  = "https://secretsafe.example.com"
  pwd      = var.secret_safe_password  # Optional
}
```

See [Provider Configuration](./docs/index.md#provider-configuration) for details.

## Documentation

Complete documentation is available in the `/docs` directory:

- **[Overview & Examples](./docs/index.md)** - Provider overview and usage examples
- **Data Sources:**
  - [`secretsafe_credential_data`](./docs/data-sources/secretsafe_credential_data.md) - Retrieve credentials
  - [`secretsafe_download_file_data`](./docs/data-sources/secretsafe_download_file_data.md) - Download file secrets
- **Resources:**
  - [`secretsafe_folder`](./docs/resources/secretsafe_folder.md) - Manage folders
  - [`secretsafe_folder_credential`](./docs/resources/secretsafe_folder_credential.md) - Manage credentials
  - [`secretsafe_folder_file`](./docs/resources/secretsafe_folder_file.md) - Manage file secrets

## Requirements

- **Terraform** >= 0.12
- **BeyondTrust Secret Safe** instance with API access
- **Linux x64** — the released `linux_amd64` binary is fully static (musl + statically-linked OpenSSL) and runs on any glibc or musl distro, including `FROM scratch` and distroless images, with no extra setup.

## Development

### Prerequisites

- **.NET 10 SDK** or later
- **Docker** (for testing and building Native AOT binaries)
- **Git**

### Building

```bash
# Restore dependencies
dotnet restore BeyondTrust.SecretSafeProvider/BeyondTrust.SecretSafeProvider.csproj

# Build (debug)
dotnet build BeyondTrust.SecretSafeProvider/BeyondTrust.SecretSafeProvider.csproj

# Run locally
dotnet run --project BeyondTrust.SecretSafeProvider/BeyondTrust.SecretSafeProvider.csproj
```

### Testing

```bash
dotnet run --project BeyondTrust.SecretSafeProvider.Tests/BeyondTrust.SecretSafeProvider.Tests.csproj \
  --disable-logo --fail-fast --no-progress --no-ansi
```

**Note:** Do NOT use `dotnet test` — it is not supported by Microsoft.Testing.Platform on .NET 10 SDK.

### Native AOT Build (Production)

Build a self-contained, trimmed binary for Linux/Alpine:

```bash
# Inside Alpine Docker container with musl + OpenSSL static toolchain
dotnet publish -c Release -r linux-musl-x64 -o /app/publish --self-contained true
```

The static linking is configured via project properties (`PublishAot`, `StaticExecutable`, `StaticOpenSslLinking`, `InvariantGlobalization`) in `BeyondTrust.SecretSafeProvider.csproj`. See [`DESIGN_DECISIONS.md`](./DESIGN_DECISIONS.md) for details.

### Project Structure

```
.
├── BeyondTrust.SecretSafeProvider/           # Main provider implementation
│   ├── Program.cs                            # gRPC server setup & handshake
│   ├── Services/
│   │   ├── Terraform5ProviderService.cs      # TPPv5.2 RPC implementations
│   │   ├── IBeyondTrustSecretSafe.cs         # Refit API client interface
│   │   ├── DataSources/                      # Data source handlers
│   │   └── Resources/                        # Resource handlers
│   ├── Models/                               # Domain models & configuration
│   └── Protos/                               # gRPC proto definitions
├── BeyondTrust.SecretSafeProvider.AppHost/   # Aspire orchestration (tests)
│   └── __admin/mappings/                     # WireMock API mocks
├── BeyondTrust.SecretSafeProvider.Tests/     # Integration & unit tests
├── docs/                                     # Terraform Registry documentation
├── CLAUDE.md                                 # Development guidelines
└── DESIGN_DECISIONS.md                       # Architecture & lessons learned
```

### Architecture

**Terraform Plugin Protocol Flow:**

1. Terraform spawns the provider binary
2. Provider writes handshake line to stdout: `1|5|tcp|{host}:{port}|grpc|{cert_base64}`
3. Provider starts gRPC server with mTLS (self-signed certificates)
4. Terraform connects via gRPC and presents its own client certificate
5. Provider implements all TPPv5.2 RPCs: `GetSchema`, `Configure`, `ReadDataSource`, `ApplyResourceChange`, etc.

**Authentication Flow:**

1. User provides `runas`, `key`, and optional `pwd` in provider configuration
2. Each operation calls `SignAppin(KeyAndRunAs)` to authenticate with BeyondTrust API
3. Returns session ID (`SID`) which is used for subsequent API calls
4. Calls `Signout()` to terminate the session

### Testing with WireMock

Tests use **Aspire** to orchestrate:
- **WireMock container** - Mocks the BeyondTrust Secret Safe HTTP API
- **Provider server** - Runs the actual provider for integration testing
- **Test client** - Calls provider gRPC methods and validates responses

WireMock mappings are located in `BeyondTrust.SecretSafeProvider.AppHost/__admin/mappings/` and define mock responses for:
- `/public/v3/Auth/SignAppin` - Authentication endpoint
- `/public/v3/Secrets-Safe/Secrets/{id}` - Get secret
- `/public/v3/Secrets-Safe/Secrets/{id}/file/download` - Download file
- `/public/v3/Secrets-Safe/Folders/*` - Folder operations

### Architecture Deep Dive

For a deeper understanding of design decisions, lessons learned, and architectural trade-offs, see [DESIGN_DECISIONS.md](./DESIGN_DECISIONS.md). This includes insights on:

- Terraform Plugin Protocol v5.2 handshake and base64 encoding without padding
- Native AOT constraints and reflection-free code requirements
- mTLS with self-signed certificates
- Service configuration patterns and dependency injection
- Testing strategies with Aspire and WireMock
- Error handling in the context of BeyondTrust API semantics

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes and ensure all tests pass
4. Commit with clear messages
5. Push to your fork and open a Pull Request

## License

MIT — see [LICENSE](./LICENSE.txt) for details.

## Support

For issues, questions, or feature requests, please open an [issue on GitHub](https://github.com/AlbertoMonteiro/terraform-provider-beyondtrust-secretsafe/issues).

For BeyondTrust Secret Safe support, visit [BeyondTrust Support](https://www.beyondtrust.com/support).
