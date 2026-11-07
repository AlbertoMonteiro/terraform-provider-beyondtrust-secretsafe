namespace BeyondTrust.SecretSafeProvider.Models;

public record CreateSecretFileRequest(
    string Title,
    string? Description,
    string FileName,
    string FileContent,
    long OwnerId,
    string? FolderId = null)
{
    // Same rule as credentials: OwnerType is required, and "User" requires Owners
    public string OwnerType => SecretOwner.USER;

    public IList<OwnerInfo> Owners => SecretOwner.Single(OwnerId);
}
