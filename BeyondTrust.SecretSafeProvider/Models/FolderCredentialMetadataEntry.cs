namespace BeyondTrust.SecretSafeProvider.Models;

// No username or password: this entry is stored in the state as plain, non-sensitive values
public record FolderCredentialMetadataEntry(
    string SecretId,
    string? CreatedOn = null,
    string? CreatedBy = null,
    string? ModifiedOn = null,
    string? ModifiedBy = null)
{
    public const string SECRET_ID   = FolderCredentialEntry.SECRET_ID;
    public const string CREATED_ON  = SecretMetadata.CREATED_ON;
    public const string CREATED_BY  = SecretMetadata.CREATED_BY;
    public const string MODIFIED_ON = SecretMetadata.MODIFIED_ON;
    public const string MODIFIED_BY = SecretMetadata.MODIFIED_BY;

    // Same order as the object type in the schema
    public static readonly string[] AttributeNames = [SECRET_ID, CREATED_ON, CREATED_BY, MODIFIED_ON, MODIFIED_BY];
}
