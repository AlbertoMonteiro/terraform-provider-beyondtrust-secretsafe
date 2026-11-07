using BeyondTrust.SecretSafeProvider.Proto;
using MessagePack;

namespace BeyondTrust.SecretSafeProvider.Models;

[MessagePackObject]
public class FileDownloadData : ISecretMetadata
{
    private const string SECRET_ID = "secret_id";
    private const string FILE_NAME = "file_name";
    private const string FILE_CONTENT_BASE64 = "file_content_base64";
    private const string FILE_HASH = "file_hash";

    [Key(SECRET_ID)]
    public required string SecretId { get; set; }

    [Key(FILE_NAME)]
    public string? FileName { get; set; }

    [Key(FILE_CONTENT_BASE64)]
    public string? FileContentBase64 { get; set; }

    [Key(FILE_HASH)]
    public string? FileHash { get; set; }

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
                    new Schema.Types.Attribute { Name = SECRET_ID, Type = TfTypes.String, Description = "The ID (GUID) of the secret file to download.", Required = true },
                    new Schema.Types.Attribute { Name = FILE_NAME,  Type = TfTypes.String, Description = "The file name from the secret.", Computed = true },
                    new Schema.Types.Attribute { Name = FILE_CONTENT_BASE64, Type = TfTypes.String, Description = "The file content encoded in base64.", Computed = true, Sensitive = true },
                    new Schema.Types.Attribute { Name = FILE_HASH, Type = TfTypes.String, Description = "The hash of the file content, as returned by Secret Safe.", Computed = true },
                    SecretMetadata.Attributes("file secret"),
                }
            }
        };
}
