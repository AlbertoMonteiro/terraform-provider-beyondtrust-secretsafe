using BeyondTrust.SecretSafeProvider.Proto;
using MessagePack;

namespace BeyondTrust.SecretSafeProvider.Models;

[MessagePackObject]
public class CredentialData : ISecretMetadata
{
    private const string SECRET_ID = "secret_id";
    private const string USERNAME  = "username";
    private const string PASSWORD  = "password";

    [Key(SECRET_ID)]
    public required string SecretId { get; set; }

    [Key(USERNAME)]
    public string? Username { get; set; }

    [Key(PASSWORD)]
    public string? Password { get; set; }

    [Key(SecretMetadata.TITLE)]
    public string? Title { get; set; }

    [Key(SecretMetadata.DESCRIPTION)]
    public string? Description { get; set; }

    [Key(SecretMetadata.FOLDER_ID)]
    public string? FolderId { get; set; }

    [Key(SecretMetadata.OWNER_ID)]
    public long? OwnerId { get; set; }

    [Key(SecretMetadata.CREATED_ON)]
    public string? CreatedOn { get; set; }

    [Key(SecretMetadata.CREATED_BY)]
    public string? CreatedBy { get; set; }

    [Key(SecretMetadata.MODIFIED_ON)]
    public string? ModifiedOn { get; set; }

    [Key(SecretMetadata.MODIFIED_BY)]
    public string? ModifiedBy { get; set; }

    [Key(SecretMetadata.OWNER)]
    public string? Owner { get; set; }

    [Key(SecretMetadata.FOLDER_PATH)]
    public string? FolderPath { get; set; }

    [Key(SecretMetadata.OWNER_TYPE)]
    public string? OwnerType { get; set; }

    [Key(SecretMetadata.NOTES)]
    public string? Notes { get; set; }

    public static Schema GetSchema()
        => new()
        {
            Version = 1,
            Block = new Schema.Types.Block
            {
                Attributes =
                {
                    new Schema.Types.Attribute { Name = SECRET_ID, Type = TfTypes.String, Description = "The ID (GUID) of the secret to retrieve.", Required = true },
                    new Schema.Types.Attribute { Name = USERNAME,  Type = TfTypes.String, Description = "The username stored in the secret.", Computed = true },
                    new Schema.Types.Attribute { Name = PASSWORD,  Type = TfTypes.String, Description = "The password stored in the secret.", Computed = true, Sensitive = true },
                    SecretMetadata.Attributes("credential"),
                }
            }
        };
}
