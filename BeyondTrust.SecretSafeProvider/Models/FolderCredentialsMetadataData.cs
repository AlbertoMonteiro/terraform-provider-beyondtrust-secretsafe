using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using Google.Protobuf;
using MessagePack;

namespace BeyondTrust.SecretSafeProvider.Models;

[MessagePackObject]
public class FolderCredentialsMetadataData
{
    private const string FOLDER_ID   = "folder_id";
    private const string FOLDER_NAME = "folder_name";
    private const string CREDENTIALS = "credentials";

    [Key(FOLDER_ID)]
    public string? FolderId { get; set; }

    [Key(FOLDER_NAME)]
    public string? FolderName { get; set; }

    [Key(CREDENTIALS)]
    [MessagePackFormatter(typeof(FolderCredentialMetadataMapFormatter))]
    public Dictionary<string, FolderCredentialMetadataEntry>? Credentials { get; set; }

    public static Schema GetSchema()
        => new()
        {
            Version = 1,
            Block = new Schema.Types.Block
            {
                Attributes =
                {
                    new Schema.Types.Attribute { Name = FOLDER_ID,   Type = TfTypes.String, Description = "The ID (GUID) of the folder. Set this or folder_name.", Optional = true, Computed = true },
                    new Schema.Types.Attribute { Name = FOLDER_NAME, Type = TfTypes.String, Description = "The name of the folder, which must match exactly one folder. Set this or folder_id.", Optional = true },
                    new Schema.Types.Attribute
                    {
                        Name = CREDENTIALS,
                        Type = TfTypes.Map(TfTypes.Object(new Dictionary<string, ByteString>
                        {
                            { FolderCredentialMetadataEntry.SECRET_ID,   TfTypes.String },
                            { FolderCredentialMetadataEntry.CREATED_ON,  TfTypes.String },
                            { FolderCredentialMetadataEntry.CREATED_BY,  TfTypes.String },
                            { FolderCredentialMetadataEntry.MODIFIED_ON, TfTypes.String },
                            { FolderCredentialMetadataEntry.MODIFIED_BY, TfTypes.String },
                        })),
                        Description = "The metadata of the credential secrets of the folder, keyed by title, without usernames or passwords. The keys are the same as in secretsafe_folder_credentials_data.",
                        Computed = true,
                    },
                }
            }
        };
}
