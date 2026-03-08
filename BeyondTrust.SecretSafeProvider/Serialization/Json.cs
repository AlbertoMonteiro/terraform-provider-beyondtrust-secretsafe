using BeyondTrust.SecretSafeProvider.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BeyondTrust.SecretSafeProvider.Serialization;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(ProviderConfiguration))]
[JsonSerializable(typeof(KeyAndRunAs))]
[JsonSerializable(typeof(SecretValue))]
[JsonSerializable(typeof(CredentialData))]
[JsonSerializable(typeof(FileDownloadData))]
[ExcludeFromCodeCoverage]
public partial class Json : JsonSerializerContext
{
}
