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
    private const string FILE_CONTENT_BASE64 = "file_content_base64";
    private const string OWNER_ID = "owner_id";
    private const string OWNERS = "owners";

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

    [Key(FILE_CONTENT_BASE64)]
    public required string FileContentBase64 { get; set; }

    [Key(OWNER_ID)]
    public required long OwnerId { get; set; }

    [Key(OWNERS)]
    public IList<OwnerInfo>? Owners { get; set; }

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
                    new Schema.Types.Attribute { Name = DESCRIPTION, Type = TfTypes.String, Description = "The description of the file secret." },
                    new Schema.Types.Attribute { Name = FILE_NAME, Type = TfTypes.String, Description = "The name of the file.", Required = true },
                    new Schema.Types.Attribute { Name = FILE_CONTENT_BASE64, Type = TfTypes.String, Description = "The file content encoded in base64.", Required = true, Sensitive = true },
                    new Schema.Types.Attribute { Name = OWNER_ID, Type = TfTypes.Number, Description = "The ID of the owner user.", Required = true },
                    new Schema.Types.Attribute { Name = OWNERS, Type = TfTypes.List(TfTypes.Object(new Dictionary<string, ByteString> { { "owner_id", TfTypes.Number } })), Description = "List of owner IDs." },
                }
            }
        };
}
