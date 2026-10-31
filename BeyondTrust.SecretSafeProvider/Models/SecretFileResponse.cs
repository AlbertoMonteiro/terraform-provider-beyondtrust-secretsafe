namespace BeyondTrust.SecretSafeProvider.Models;

public record SecretFileResponse(
    string? Id = null,
    string? Title = null,
    string? Description = null,
    string? Username = null,
    long OwnerId = default,
    string? FolderId = null,
    string? CreatedOn = null,
    string? CreatedBy = null,
    string? ModifiedOn = null,
    string? ModifiedBy = null,
    string? Owner = null,
    string? Folder = null,
    string? FolderPath = null,
    List<OwnerInfo>? Owners = null,
    string? OwnerType = null,
    string? Notes = null,
    string? FileName = null,
    string? FileHash = null) : SecretResponse(
    Id, Title, Description, Username, OwnerId, FolderId,
    CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, Owner, Folder, FolderPath, Owners, OwnerType, Notes);
