using BeyondTrust.SecretSafeProvider.Models;
using System.Text.Json.Serialization;

namespace BeyondTrust.SecretSafeProvider.Services;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(ProviderConfiguration))]
[JsonSerializable(typeof(KeyAndRunAs))]
[JsonSerializable(typeof(SecretValue))]
[JsonSerializable(typeof(CredentialData))]
public partial class Json : JsonSerializerContext
{
}
