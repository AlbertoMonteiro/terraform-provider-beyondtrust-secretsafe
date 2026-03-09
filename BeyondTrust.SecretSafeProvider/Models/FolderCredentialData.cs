using BeyondTrust.SecretSafeProvider.Proto;
using Google.Protobuf;
using MessagePack;

namespace BeyondTrust.SecretSafeProvider.Models;

[MessagePackObject]
public class FolderCredentialData
{
    private const string ID = "id";
    private const string FOLDER_ID = "folder_id";
    private const string TITLE = "title";
    private const string DESCRIPTION = "description";
    private const string USERNAME = "username";
    private const string PASSWORD = "password";
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

    [Key(USERNAME)]
    public required string Username { get; set; }

    [Key(PASSWORD)]
    public required string Password { get; set; }

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
                    new Schema.Types.Attribute { Name = ID, Type = TfTypes.String, Description = "The ID (GUID) of the created credential secret.", Computed = true },
                    new Schema.Types.Attribute { Name = FOLDER_ID, Type = TfTypes.String, Description = "The ID (GUID) of the folder where the credential will be stored.", Required = true },
                    new Schema.Types.Attribute { Name = TITLE, Type = TfTypes.String, Description = "The title of the credential secret.", Required = true },
                    new Schema.Types.Attribute { Name = DESCRIPTION, Type = TfTypes.String, Description = "The description of the credential secret." },
                    new Schema.Types.Attribute { Name = USERNAME, Type = TfTypes.String, Description = "The username for the credential.", Required = true },
                    new Schema.Types.Attribute { Name = PASSWORD, Type = TfTypes.String, Description = "The password for the credential.", Required = true, Sensitive = true },
                    new Schema.Types.Attribute { Name = OWNER_ID, Type = TfTypes.Number, Description = "The ID of the owner user (automatically set to authenticated user).", Computed = true },
                    new Schema.Types.Attribute { Name = OWNERS, Type = TfTypes.List(TfTypes.Object(new Dictionary<string, ByteString> { { "owner_id", TfTypes.Number } })), Description = "List of owner IDs (computed from authenticated user).", Computed = true },
                }
            }
        };
}
