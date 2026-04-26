using BeyondTrust.SecretSafeProvider.Proto;
using Google.Protobuf;
using MessagePack;

namespace BeyondTrust.SecretSafeProvider.Models;

[MessagePackObject]
public class FolderFileSecretData
{
    private const string ID = "id";
    private const string FOLDER_ID = "folder_id";
    private const string TITLE = "title";
    private const string DESCRIPTION = "description";
    private const string FILE_NAME = "file_name";
    private const string FILE_HASH = "file_hash";
    private const string FILE_CONTENT_BASE64 = "file_content_base64";
    private const string OWNER_ID = "owner_id";
    private const string OWNERS = "owners";
    private const string CREATED_ON = "created_on";
    private const string CREATED_BY = "created_by";
    private const string MODIFIED_ON = "modified_on";
    private const string MODIFIED_BY = "modified_by";
    private const string OWNER = "owner";
    private const string FOLDER = "folder";
    private const string FOLDER_PATH = "folder_path";
    private const string OWNER_TYPE = "owner_type";
    private const string NOTES = "notes";

    [Key(ID)]
    public string? Id { get; set; }

    [Key(FOLDER_ID)]
    public required string FolderId { get; set; }

    [Key(TITLE)]
    public required string Title { get; set; }

    [Key(DESCRIPTION)]
    public string? Description { get; set; }

    [Key(FILE_NAME)]
    public required string FileName { get; set; }

    [Key(FILE_HASH)]
    public string? FileHash { get; set; }

    [Key(FILE_CONTENT_BASE64)]
    public required string FileContentBase64 { get; set; }

    [Key(OWNER_ID)]
    public required long OwnerId { get; set; }

    [Key(OWNERS)]
    public IList<OwnerInfo>? Owners { get; set; }

    [Key(CREATED_ON)]
    public DateTime? CreatedOn { get; set; }

    [Key(CREATED_BY)]
    public string? CreatedBy { get; set; }

    [Key(MODIFIED_ON)]
    public DateTime? ModifiedOn { get; set; }

    [Key(MODIFIED_BY)]
    public string? ModifiedBy { get; set; }

    [Key(OWNER)]
    public string? Owner { get; set; }

    [Key(FOLDER)]
    public string? Folder { get; set; }

    [Key(FOLDER_PATH)]
    public string? FolderPath { get; set; }

    [Key(OWNER_TYPE)]
    public string? OwnerType { get; set; }

    [Key(NOTES)]
    public string? Notes { get; set; }

    public static Schema GetSchema()
        => new()
        {
            Version = 1,
            Block = new Schema.Types.Block
            {
                Attributes =
                {
                    new Schema.Types.Attribute { Name = ID, Type = TfTypes.String, Description = "The ID (GUID) of the created file secret.", Computed = true },
                    new Schema.Types.Attribute { Name = FOLDER_ID, Type = TfTypes.String, Description = "The ID (GUID) of the folder where the file will be stored.", Required = true },
                    new Schema.Types.Attribute { Name = TITLE, Type = TfTypes.String, Description = "The title of the file secret.", Required = true },
                    new Schema.Types.Attribute { Name = DESCRIPTION, Type = TfTypes.String, Description = "The description of the file secret.", Optional = true },
                    new Schema.Types.Attribute { Name = FILE_NAME, Type = TfTypes.String, Description = "The name of the file.", Required = true },
                    new Schema.Types.Attribute { Name = FILE_HASH, Type = TfTypes.String, Description = "The hash of the file content.", Computed = true },
                    new Schema.Types.Attribute { Name = FILE_CONTENT_BASE64, Type = TfTypes.String, Description = "The file content encoded in base64.", Required = true, Sensitive = true },
                    new Schema.Types.Attribute { Name = OWNER_ID, Type = TfTypes.Number, Description = "The ID of the owner user (automatically set to authenticated user).", Computed = true },
                    new Schema.Types.Attribute { Name = OWNERS, Type = TfTypes.List(TfTypes.Object(new Dictionary<string, ByteString> { { "owner_id", TfTypes.Number } })), Description = "List of owner IDs (computed from authenticated user).", Computed = true },
                    new Schema.Types.Attribute { Name = CREATED_ON, Type = TfTypes.String, Description = "The timestamp when the file was created.", Computed = true },
                    new Schema.Types.Attribute { Name = CREATED_BY, Type = TfTypes.String, Description = "The user who created the file.", Computed = true },
                    new Schema.Types.Attribute { Name = MODIFIED_ON, Type = TfTypes.String, Description = "The timestamp when the file was last modified.", Computed = true },
                    new Schema.Types.Attribute { Name = MODIFIED_BY, Type = TfTypes.String, Description = "The user who last modified the file.", Computed = true },
                    new Schema.Types.Attribute { Name = OWNER, Type = TfTypes.String, Description = "The username of the file owner.", Computed = true },
                    new Schema.Types.Attribute { Name = FOLDER, Type = TfTypes.String, Description = "The name of the folder containing the file.", Computed = true },
                    new Schema.Types.Attribute { Name = FOLDER_PATH, Type = TfTypes.String, Description = "The full path of the folder containing the file.", Computed = true },
                    new Schema.Types.Attribute { Name = OWNER_TYPE, Type = TfTypes.String, Description = "The type of the file owner (e.g., 'User').", Computed = true },
                    new Schema.Types.Attribute { Name = NOTES, Type = TfTypes.String, Description = "Notes associated with the file.", Computed = true },
                }
            }
        };
}
