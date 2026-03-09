using BeyondTrust.SecretSafeProvider.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BeyondTrust.SecretSafeProvider.Serialization;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(ProviderConfiguration))]
[JsonSerializable(typeof(KeyAndRunAs))]
[JsonSerializable(typeof(SecretValue))]
[JsonSerializable(typeof(SignAppinResponse))]
[JsonSerializable(typeof(CredentialData))]
[JsonSerializable(typeof(FileDownloadData))]
[JsonSerializable(typeof(CreateSecretCredentialRequest))]
[JsonSerializable(typeof(OwnerInfo))]
[JsonSerializable(typeof(SecretResponse))]
[JsonSerializable(typeof(FolderCredentialData))]
[ExcludeFromCodeCoverage]
public partial class Json : JsonSerializerContext
{
}
