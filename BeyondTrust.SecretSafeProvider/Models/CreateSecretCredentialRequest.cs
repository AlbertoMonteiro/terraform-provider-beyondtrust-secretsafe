using System.Text.Json.Serialization;

namespace BeyondTrust.SecretSafeProvider.Models;

public record CreateSecretCredentialRequest(
    string Title,
    string? Description,
    string Username,
    string Password,
    long OwnerId,
    IList<OwnerInfo>? Owners = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? FolderId = null);

public record OwnerInfo(long OwnerId);
