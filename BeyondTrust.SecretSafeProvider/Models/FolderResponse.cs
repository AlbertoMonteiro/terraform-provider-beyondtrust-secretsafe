namespace BeyondTrust.SecretSafeProvider.Models;

public record FolderResponse(
    string Id,
    string Name,
    string? Description,
    string? ParentId,
    long UserGroupId);
