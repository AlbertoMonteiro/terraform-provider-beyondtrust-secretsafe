using System.Text.Json.Serialization;
using BeyondTrust.SecretSafeProvider.Proto;
using MessagePack;

namespace BeyondTrust.SecretSafeProvider.Models;

[MessagePackObject]
public class ProviderConfiguration
{
    private const string RUN_AS = "runas";
    private const string KEY = "key";
    private const string BASE_URL = "baseUrl";
    private const string PWD = "pwd";
    private const string CLIENT_ID = "client_id";
    private const string CLIENT_SECRET = "client_secret";

    [Key(RUN_AS)]
    public string? RunAs { get; set; }

    [Key(KEY)]
    public string? Key { get; set; }

    [Key(BASE_URL)]
    public required string BaseUrl { get; set; }

    [Key(PWD)]
    public string? Pwd { get; set; }

    [Key(CLIENT_ID)]
    public string? ClientId { get; set; }

    [Key(CLIENT_SECRET)]
    public string? ClientSecret { get; set; }

    [IgnoreMember]
    [JsonIgnore]
    public bool UsesOAuth => !string.IsNullOrEmpty(ClientId) || !string.IsNullOrEmpty(ClientSecret);

    public void ReplaceValues(ProviderConfiguration configuration)
        => (RunAs, Key, BaseUrl, Pwd, ClientId, ClientSecret) = (configuration.RunAs, configuration.Key, configuration.BaseUrl, configuration.Pwd, configuration.ClientId, configuration.ClientSecret);

    public IReadOnlyList<string> Validate()
    {
        List<string> errors = [];

        if (UsesOAuth)
        {
            if (string.IsNullOrEmpty(ClientId))
                errors.Add($"\"{CLIENT_ID}\" is required when \"{CLIENT_SECRET}\" is set.");

            if (string.IsNullOrEmpty(ClientSecret))
                errors.Add($"\"{CLIENT_SECRET}\" is required when \"{CLIENT_ID}\" is set.");

            foreach (var (name, value) in new[] { (KEY, Key), (RUN_AS, RunAs), (PWD, Pwd) })
                if (!string.IsNullOrEmpty(value))
                    errors.Add($"\"{name}\" cannot be combined with \"{CLIENT_ID}\"/\"{CLIENT_SECRET}\".");

            return errors;
        }

        if (string.IsNullOrEmpty(Key))
            errors.Add($"\"{KEY}\" is required when \"{CLIENT_ID}\"/\"{CLIENT_SECRET}\" are not set.");

        if (string.IsNullOrEmpty(RunAs))
            errors.Add($"\"{RUN_AS}\" is required when \"{CLIENT_ID}\"/\"{CLIENT_SECRET}\" are not set.");

        return errors;
    }

    public static Schema GetSchema()
        => new()
        {
            Version = 1,
            Block = new Schema.Types.Block()
            {
                Attributes =
                {
                    new Schema.Types.Attribute() { Name = RUN_AS, Type = TfTypes.String, Description = "User to authenticate in BeyondTrust Secret Safe. Required with key", Optional = true },
                    new Schema.Types.Attribute() { Name = KEY, Type = TfTypes.String, Description = "The api key of BeyondTrust Secret Safe. Required when client_id and client_secret are not set", Optional = true, Sensitive = true},
                    new Schema.Types.Attribute() { Name = BASE_URL, Type = TfTypes.String, Description = "Base url from BeyondTrust Secret Safe", Required = true },
                    new Schema.Types.Attribute() { Name = PWD, Type = TfTypes.String, Description = "Optional domain password for BeyondTrust Secret Safe authentication", Optional = true, Sensitive = true },
                    new Schema.Types.Attribute() { Name = CLIENT_ID, Type = TfTypes.String, Description = "OAuth client id of BeyondTrust Secret Safe. Required with client_secret, cannot be combined with key, runas or pwd", Optional = true, Sensitive = true },
                    new Schema.Types.Attribute() { Name = CLIENT_SECRET, Type = TfTypes.String, Description = "OAuth client secret of BeyondTrust Secret Safe. Required with client_id, cannot be combined with key, runas or pwd", Optional = true, Sensitive = true },
                }
            },
        };
}
