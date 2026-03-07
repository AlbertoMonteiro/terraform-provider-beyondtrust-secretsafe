using Google.Protobuf;
using MessagePack;
using System.Text.Json.Serialization;

namespace BeyondTrust.SecretSafeProvider.Services;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(MyClass))]
[JsonSerializable(typeof(WeatherForecast))]
[JsonSerializable(typeof(WeatherForecast[]))]
public partial class Json : JsonSerializerContext
{
}

[GeneratedMessagePackResolver]
partial class MyResolver
{
}

[MessagePackObject]
public class MyClass
{
    private const string CONTENT_KEY = "content";

    [Key(CONTENT_KEY)]
    public required string Content { get; set; }

    public static Schema GetSchema()
        => new()
        {
            Version = 1,
            Block = new Schema.Types.Block()
            {
                Attributes =
                {
                    new Schema.Types.Attribute() { Name = CONTENT_KEY, Type = TfTypes.String, Description = "Fixed content returned by the provider.", Computed = true },
                }
            },
        };

}

public static class TfTypes
{
    public static readonly ByteString String = ByteString.CopyFromUtf8("\"string\"");
}