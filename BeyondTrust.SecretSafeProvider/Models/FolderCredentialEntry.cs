namespace BeyondTrust.SecretSafeProvider.Models;

public record FolderCredentialEntry(
    string SecretId,
    string Username,
    string Password,
    string? CreatedOn = null,
    string? CreatedBy = null,
    string? ModifiedOn = null,
    string? ModifiedBy = null)
{
    public const string SECRET_ID   = "secret_id";
    public const string USERNAME    = "username";
    public const string PASSWORD    = "password";
    public const string CREATED_ON  = SecretMetadata.CREATED_ON;
    public const string CREATED_BY  = SecretMetadata.CREATED_BY;
    public const string MODIFIED_ON = SecretMetadata.MODIFIED_ON;
    public const string MODIFIED_BY = SecretMetadata.MODIFIED_BY;

    // Same order as the object type in the schema
    public static readonly string[] AttributeNames = [SECRET_ID, USERNAME, PASSWORD, CREATED_ON, CREATED_BY, MODIFIED_ON, MODIFIED_BY];
}
