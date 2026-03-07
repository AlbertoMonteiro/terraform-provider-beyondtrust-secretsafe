using BeyondTrust.SecretSafeProvider.Services;
using Google.Protobuf;
using MessagePack;
using System.Text.Json;

namespace BeyondTrust.SecretSafeProvider.Serialization;

public static class SmartSerializer
{
    public static T Deserialize<T>(DynamicValue dynamicValue)
    {
        var data = dynamicValue switch
        {
            { Msgpack.IsEmpty: false } => MessagePackSerializer.Deserialize<T>(dynamicValue.Msgpack.Memory),
            { Json.IsEmpty: false } => JsonSerializer.Deserialize<T>(dynamicValue.Json.Span, Json.Default.Options)!
        };

        return data;
    }

    public static DynamicValue Serialize<T>(T @object)
        => new()
        {
            Msgpack = ByteString.CopyFrom(MessagePackSerializer.Serialize(@object)),
            Json = ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes(@object, Json.Default.Options))
        };
}
