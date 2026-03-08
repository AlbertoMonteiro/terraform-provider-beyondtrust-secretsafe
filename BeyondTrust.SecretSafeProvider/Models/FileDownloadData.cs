using BeyondTrust.SecretSafeProvider.Proto;
using MessagePack;

namespace BeyondTrust.SecretSafeProvider.Models;

[MessagePackObject]
public class FileDownloadData
{
    private const string SECRET_ID = "secret_id";
    private const string FILE_NAME = "file_name";
    private const string FILE_CONTENT_BASE64 = "file_content_base64";

    [Key(SECRET_ID)]
    public required string SecretId { get; set; }

    [Key(FILE_NAME)]
    public string? FileName { get; set; }

    [Key(FILE_CONTENT_BASE64)]
    public string? FileContentBase64 { get; set; }

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
                }
            }
        };
}
