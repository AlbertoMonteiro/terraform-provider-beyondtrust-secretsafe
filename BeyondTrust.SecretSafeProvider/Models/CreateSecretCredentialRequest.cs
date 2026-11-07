using System.Text.Json.Serialization;

namespace BeyondTrust.SecretSafeProvider.Models;

public record CreateSecretCredentialRequest(
    string Title,
    string? Description,
    string Username,
    string Password,
    long OwnerId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? FolderId = null)
{
    // The API rejects a create or update without OwnerType ("Invalid OwnerType"), and with "User" it requires Owners
    public string OwnerType => SecretOwner.USER;

    public IList<OwnerInfo> Owners => SecretOwner.Single(OwnerId);
}

public record OwnerInfo(long OwnerId);

public static class SecretOwner
{
    public const string USER = "User";

    public static IList<OwnerInfo> Single(long ownerId) => [new OwnerInfo(ownerId)];
}
