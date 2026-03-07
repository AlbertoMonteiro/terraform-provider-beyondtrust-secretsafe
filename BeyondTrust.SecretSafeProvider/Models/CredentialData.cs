using MessagePack;

namespace BeyondTrust.SecretSafeProvider.Models;

[MessagePackObject]
public class CredentialData
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
                }
            }
        };
}
