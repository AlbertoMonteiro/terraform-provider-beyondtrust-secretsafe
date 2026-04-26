using BeyondTrust.SecretSafeProvider.Proto;
using MessagePack;

namespace BeyondTrust.SecretSafeProvider.Models;

[MessagePackObject]
public class FolderResourceData
{
    private const string ID = "id";
    private const string NAME = "name";
    private const string DESCRIPTION = "description";
    private const string OWNER_ID = "owner_id";
    private const string PARENT_ID = "parent_id";
    private const string USER_GROUP_ID = "user_group_id";

    [Key(ID)]
    public string? Id { get; set; }

    [Key(NAME)]
    public required string Name { get; set; }

    [Key(DESCRIPTION)]
    public string? Description { get; set; }

    [Key(OWNER_ID)]
    public long? OwnerId { get; set; }

    [Key(PARENT_ID)]
    public string? ParentId { get; set; }

    [Key(USER_GROUP_ID)]
    public required long UserGroupId { get; set; }

    public static Schema GetSchema()
        => new()
        {
            Version = 1,
            Block = new Schema.Types.Block
            {
                Attributes =
                {
                    new Schema.Types.Attribute { Name = ID, Type = TfTypes.String, Description = "The ID (GUID) of the folder.", Computed = true },
                    new Schema.Types.Attribute { Name = NAME, Type = TfTypes.String, Description = "The name of the folder.", Required = true },
                    new Schema.Types.Attribute { Name = DESCRIPTION, Type = TfTypes.String, Description = "The description of the folder.", Optional = true },
                    new Schema.Types.Attribute { Name = OWNER_ID, Type = TfTypes.Number, Description = "The ID of the owner user. If not specified, defaults to the authenticated user.", Optional = true },
                    new Schema.Types.Attribute { Name = PARENT_ID, Type = TfTypes.String, Description = "The ID of the parent folder.", Optional = true },
                    new Schema.Types.Attribute { Name = USER_GROUP_ID, Type = TfTypes.Number, Description = "The ID of the user group that owns the folder.", Required = true },
                }
            }
        };
}
