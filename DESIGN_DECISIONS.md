# Design Decisions & Lessons Learned

This document captures key architectural decisions and lessons learned during the development of the BeyondTrust Secret Safe Terraform provider.

## Terraform Plugin Protocol v5.2 Handshake

### The Handshake Line

When Terraform spawns the provider binary, it expects a single line on stdout before doing anything else. This line follows the format:

```
1|5|tcp|{host}:{port}|grpc|{tls_cert_base64}
```

Fields (pipe-delimited):
- `core_protocol_version` = `1`
- `plugin_protocol_version` = `5` (TPPv5.2)
- `network` = `tcp`
- `address` = `{host}:{port}` where the gRPC server is listening
- `protocol` = `grpc`
- `tls_cert` = Base64-encoded DER certificate (see below)

**Critical:** Nothing else may be written to stdout before this line, or Terraform will fail to parse the handshake.

### Base64 Encoding Without Padding

The certificate in the handshake must be base64-encoded **without padding**. This is because:

1. **go-plugin uses `base64.RawStdEncoding`** - The official HashiCorp go-plugin library (which Terraform uses) encodes certificates using raw standard encoding, which omits trailing `=` padding characters
2. **Provider must match this** - Our provider must remove trailing `=` characters to match Terraform's expectations

**Implementation:**

```csharp
var cert = Convert.ToBase64String(certificate.RawData)
    .Replace("\r", "")       // Remove any CR chars
    .Replace("\n", "")       // Remove any LF chars
    .TrimEnd('=');           // Remove trailing padding
var line = $"1|5|tcp|{address}|grpc|{cert}\n";
var bytes = System.Text.Encoding.ASCII.GetBytes(line);

using var stdout = Console.OpenStandardOutput();
stdout.Write(bytes);
stdout.Flush();
```

**Why this matters:** If padding characters are included, Terraform will fail to decode the certificate and refuse to connect. The error is cryptic ("certificate parse error") and difficult to debug without understanding this requirement.

## Native AOT Constraints

### Reflection-Free Code

Native AOT compilation with `PublishAot=true` and `TrimMode=full` means:

- **No reflection** - Cannot use `Type.GetType()`, `MethodInfo.Invoke()`, or similar runtime type inspection
- **No dynamic dispatch** - All method calls must be resolvable at compile-time
- **Serialization generators** - Must use source generators for JSON/MessagePack serialization

**Impact on our design:**
- Use `System.Text.Json` with `JsonSourceGenerationContext` for JSON serialization
- Use `MessagePack` with code generation for binary serialization
- Explicitly register all service types in dependency injection at startup
- Avoid generic reflection patterns in gRPC service implementation

### Binary Size & Startup Performance

With `StaticExecutable=true` and `OptimizationPreference=Size`:

- Final binary is ~15 MB (fully self-contained, no .NET runtime needed)
- Startup is sub-second (no JIT, no assembly loading)
- No external dependencies in the final artifact

**Trade-off:** Slightly longer build time (compilation to native code) for dramatically faster runtime performance.

## mTLS with Self-Signed Certificates

### Why Self-Signed?

The provider generates its own self-signed certificate at startup because:

1. **Trust is implicit** - Terraform spawns the provider; both are on the same machine
2. **No CA overhead** - No need for certificate authority or key management infrastructure
3. **Provider controls its own identity** - Each provider instance can have a unique certificate

### Certificate Lifecycle

1. Provider generates a new self-signed certificate on each startup (no persistence)
2. Certificate is embedded in the handshake and sent to Terraform
3. Terraform uses that specific certificate for all subsequent gRPC calls
4. Certificate is discarded when provider shuts down

### AllowAnyClientCertificate()

The server is configured with `AllowAnyClientCertificate()` because:

- Terraform presents a client certificate as part of mTLS
- We don't need to validate this certificate (Terraform is the only client)
- This simplifies certificate management at the cost of trusting that Terraform is running locally

## Slim Builder vs Full WebApplication Builder

### Debug Mode

In `DEBUG` configuration, we use the full `WebApplication.CreateBuilder()` because:

- Better logging and diagnostics during development
- Easier to attach debuggers
- Allows for extension and middleware addition if needed

### Release / AOT Mode

In `RELEASE` configuration, we use `WebApplication.CreateSlimBuilder()` because:

- Slim builder is compatible with Native AOT compilation
- Removes unnecessary middleware and features not used by gRPC services
- Results in smaller binary and faster startup

**Code pattern:**

```csharp
#if DEBUG
var builder = WebApplication.CreateBuilder(args);
#else
var builder = WebApplication.CreateSlimBuilder(args);
#endif
```

## Service Discovery & Configuration

### Singleton ProviderConfiguration

The `ProviderConfiguration` is registered as a singleton in DI because:

1. **Terraform calls `Configure` once** at initialization
2. **Configuration is shared** across all subsequent RPC calls
3. **Thread-safe mutations** via `ReplaceValues()` method

The singleton pattern ensures that when `Terraform5ProviderService.Configure()` is called, it updates the shared configuration object that all handlers see.

### API Client per Request

The `IBeyondTrustSecretSafe` API client is **NOT** singleton; it's created on-demand via `IBeyondTrustApiFactory` because:

1. **Session isolation** - Each operation gets a fresh authentication session
2. **No connection pooling needed** - gRPC/HTTP/2 handles multiplexing
3. **Clean separation** - Each operation is independent and self-contained

## Testing Strategy

### Aspire for Integration Tests

We use **Aspire** (`DistributedApplicationFactory`) for orchestrating integration tests because:

1. **Realistic environment** - Spins up actual provider server and mock API
2. **WireMock mocking** - No need to modify code for testing
3. **Clean isolation** - Each test run has fresh containers
4. **Readable test code** - Test infrastructure is declarative and clear

### WireMock for API Mocking

WireMock is used instead of in-memory mocking because:

1. **HTTP fidelity** - Tests exercise the real Refit HTTP client
2. **Exact API behavior** - Can mock status codes, headers, delays, etc.
3. **Easy to inspect** - WireMock admin API lets us verify requests
4. **Persisted mappings** - Mappings are JSON files, not C# code (DRY)

### Mapping Format and Pitfalls

WireMock mapping files have specific JSON structure requirements:

- `Request.Headers` must be an object, NOT an array
- `Response.Headers` must be an object
- `Request.Path` can be a string OR an object with matchers
- `Response.Body` is for plain text; `Response.BodyAsJson` for JSON

**Learning:** A typo in the header format caused WireMock to silently skip the entire mapping, resulting in 404 errors that were extremely hard to debug. Always validate mapping JSON against WireMock's schema.

## Refit HTTP Client

### Authorize Attribute Considerations

The `[Authorize("PS-Auth")]` attribute in Refit is designed for bearer token patterns. For custom authorization schemes:

- The attribute name becomes the scheme name
- Custom `AuthenticationHandler` middleware would be needed for non-standard schemes
- For simple cases, consider using headers directly instead of the attribute

### Multipart File Uploads

File uploads to BeyondTrust Secret Safe use multipart form data. With Refit:

```csharp
[Multipart]
[Post("{endpoint}")]
public Task<SecretResponse> CreateFileSecret(
    string folderId,
    [AliasAs("SecretMetadata")] CreateSecretFileRequest metadata,
    [AliasAs("File")] StreamPart file);
```

The `[AliasAs]` attribute maps C# parameter names to form field names, allowing structured request bodies alongside file uploads.

## Error Handling

### SignAppin Failure = Cascading Failures

In all operations, if `SignAppin` fails, subsequent API calls will fail. Therefore:

1. **Fail fast on auth failure** - Don't attempt to recover
2. **Return diagnostic** - Include the SignAppin error in the response
3. **Clean up gracefully** - Don't call Signout if SignAppin failed

### Folder Deletion with Conflicts

Attempting to delete a folder with secrets returns HTTP 409 Conflict. This is expected behavior and must be handled:

```csharp
if (deleteResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
{
    return new ApplyResourceChange.Types.Response
    {
        Diagnostics = { new Diagnostic { ... } }
    };
}
```

The 409 is not an error; it's a feature to prevent data loss.

## Summary

Key takeaways for maintaining and extending this provider:

1. **Handshake is critical** - The base64 padding issue is non-obvious and breaks silently
2. **AOT has limits** - No reflection means explicit wiring and source generation
3. **Testing needs real infrastructure** - Integration tests with Aspire + WireMock are worth the complexity
4. **Configuration as singleton** - Shared mutable state requires careful coordination
5. **Error handling is domain-specific** - Understand BeyondTrust API semantics (e.g., 409 = expected)
