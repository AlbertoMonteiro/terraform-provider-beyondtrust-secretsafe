namespace BeyondTrust.SecretSafeProvider.Models;

public record CreateSecretFileRequest(
    string Title,
    string? Description,
    string FileName,
    string FileContent,
    long OwnerId,
    IList<OwnerInfo>? Owners = null);
