using MessagePack;

namespace BeyondTrust.SecretSafeProvider.Models;

public record CreateSecretCredentialRequest(
    string Title,
    string? Description,
    string Username,
    string Password,
    long OwnerId,
    IList<OwnerInfo>? Owners = null);

[MessagePackObject]
public record OwnerInfo(
    [property: Key(0)]
    long OwnerId);
