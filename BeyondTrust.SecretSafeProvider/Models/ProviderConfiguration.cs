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

    [Key(RUN_AS)]
    public required string RunAs { get; set; }

    [Key(KEY)]
    public required string Key { get; set; }

    [Key(BASE_URL)]
    public required string BaseUrl { get; set; }

    [Key(PWD)]
    public string? Pwd { get; set; }

    public void ReplaceValues(ProviderConfiguration configuration)
        => (RunAs, Key, BaseUrl, Pwd) = (configuration.RunAs, configuration.Key, configuration.BaseUrl, configuration.Pwd);

    public static Schema GetSchema()
        => new()
        {
            Version = 1,
            Block = new Schema.Types.Block()
            {
                Attributes =
                {
                    new Schema.Types.Attribute() { Name = RUN_AS, Type = TfTypes.String, Description = "User to authenticate in BeyondTrust Secret Safe", Required = true },
                    new Schema.Types.Attribute() { Name = KEY, Type = TfTypes.String, Description = "The api key of BeyondTrust Secret Safe", Required = true, Sensitive = true},
                    new Schema.Types.Attribute() { Name = BASE_URL, Type = TfTypes.String, Description = "Base url from BeyondTrust Secret Safe", Required = true },
                    new Schema.Types.Attribute() { Name = PWD, Type = TfTypes.String, Description = "Optional domain password for BeyondTrust Secret Safe authentication", Optional = true, Sensitive = true },
                }
            },
        };
}
