namespace BeyondTrust.SecretSafeProvider.Models;

public record FolderRequest(
    string Name,
    string? Description,
    string? ParentId,
    long UserGroupId);
