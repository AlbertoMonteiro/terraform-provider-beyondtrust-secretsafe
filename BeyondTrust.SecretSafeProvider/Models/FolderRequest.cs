namespace BeyondTrust.SecretSafeProvider.Models;

public record FolderRequest(
    long? OwnerId,
    string Name,
    string? Description,
    string? ParentId,
    long UserGroupId);
