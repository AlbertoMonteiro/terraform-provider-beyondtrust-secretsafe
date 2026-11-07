using BeyondTrust.SecretSafeProvider.Proto;

namespace BeyondTrust.SecretSafeProvider.Models;

/// <summary>
/// Read-only metadata that Secret Safe returns for every secret. The data models keep their own
/// [Key] properties, so MessagePack does not depend on inherited members under Native AOT.
/// </summary>
public interface ISecretMetadata
{
    string? Title { get; set; }
    string? Description { get; set; }
    string? FolderId { get; set; }
    long? OwnerId { get; set; }
    string? CreatedOn { get; set; }
    string? CreatedBy { get; set; }
    string? ModifiedOn { get; set; }
    string? ModifiedBy { get; set; }
    string? Owner { get; set; }
    string? FolderPath { get; set; }
    string? OwnerType { get; set; }
    string? Notes { get; set; }
}

public static class SecretMetadata
{
    public const string TITLE = "title";
    public const string DESCRIPTION = "description";
    public const string FOLDER_ID = "folder_id";
    public const string OWNER_ID = "owner_id";
    public const string CREATED_ON = "created_on";
    public const string CREATED_BY = "created_by";
    public const string MODIFIED_ON = "modified_on";
    public const string MODIFIED_BY = "modified_by";
    public const string OWNER = "owner";
    public const string FOLDER_PATH = "folder_path";
    public const string OWNER_TYPE = "owner_type";
    public const string NOTES = "notes";

    public static IEnumerable<Schema.Types.Attribute> Attributes(string secretKind)
    {
        yield return Computed(TITLE, TfTypes.String, $"The title of the {secretKind}.");
        yield return Computed(DESCRIPTION, TfTypes.String, $"The description of the {secretKind}.");
        yield return Computed(FOLDER_ID, TfTypes.String, "The ID of the parent folder.");
        yield return Computed(OWNER_ID, TfTypes.Number, "The ID of the secret owner.");
        yield return Computed(CREATED_ON, TfTypes.String, $"When the {secretKind} was created, in ISO 8601 UTC as returned by Secret Safe.");
        yield return Computed(CREATED_BY, TfTypes.String, $"The user who created the {secretKind}.");
        yield return Computed(MODIFIED_ON, TfTypes.String, $"When the {secretKind} was last modified, in ISO 8601 UTC as returned by Secret Safe.");
        yield return Computed(MODIFIED_BY, TfTypes.String, $"The user who last modified the {secretKind}.");
        yield return Computed(OWNER, TfTypes.String, "The name of the secret owner.");
        yield return Computed(FOLDER_PATH, TfTypes.String, "The full path of the parent folder.");
        yield return Computed(OWNER_TYPE, TfTypes.String, "The type of the owner, such as User or Group.");
        yield return Computed(NOTES, TfTypes.String, $"The notes of the {secretKind}.");
    }

    public static T CopyFrom<T>(this T target, SecretValue secret) where T : ISecretMetadata
    {
        target.Title = secret.Title;
        target.Description = secret.Description;
        target.FolderId = secret.FolderId;
        target.OwnerId = secret.OwnerId;
        target.CreatedOn = secret.CreatedOn;
        target.CreatedBy = secret.CreatedBy;
        target.ModifiedOn = secret.ModifiedOn;
        target.ModifiedBy = secret.ModifiedBy;
        target.Owner = secret.Owner;
        target.FolderPath = secret.FolderPath;
        target.OwnerType = secret.OwnerType;
        target.Notes = secret.Notes;
        return target;
    }

    private static Schema.Types.Attribute Computed(string name, Google.Protobuf.ByteString type, string description)
        => new() { Name = name, Type = type, Description = description, Computed = true };
}
