using System.Buffers;
using BeyondTrust.SecretSafeProvider.Proto;
using Google.Protobuf;
using MessagePack;

namespace BeyondTrust.SecretSafeProvider.Serialization;

/// <summary>
/// Reads a msgpack encoded Terraform object as raw attribute values, so a plan can be changed
/// without knowing the resource type. Unknown values are msgpack extensions, which the typed
/// models cannot deserialize.
/// </summary>
public static class TerraformObject
{
    private static readonly byte[] Nil = [MessagePackCode.Nil];
    private static readonly byte[] Unknown = [MessagePackCode.FixExt1, 0, 0];

    public static Dictionary<string, byte[]>? Read(DynamicValue value)
    {
        if (value is null || value.Msgpack.IsEmpty)
            return null;

        var reader = new MessagePackReader(value.Msgpack.Memory);

        if (reader.TryReadNil())
            return null;

        var count = reader.ReadMapHeader();
        var attributes = new Dictionary<string, byte[]>(count);

        for (var i = 0; i < count; i++)
        {
            var name = reader.ReadString()!;
            attributes[name] = reader.ReadRaw().ToArray();
        }

        return attributes;
    }

    public static DynamicValue Write(Dictionary<string, byte[]> attributes)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);

        writer.WriteMapHeader(attributes.Count);

        foreach (var (name, value) in attributes)
        {
            writer.Write(name);
            writer.WriteRaw(value);
        }

        writer.Flush();

        return new DynamicValue { Msgpack = ByteString.CopyFrom(buffer.WrittenSpan) };
    }

    public static void MarkUnknown(Dictionary<string, byte[]> attributes, string name)
        => attributes[name] = Unknown;

    public static bool IsNull(Dictionary<string, byte[]>? attributes, string name)
        => attributes is null || !attributes.TryGetValue(name, out var value) || value.SequenceEqual(Nil);

    public static bool HaveSameValue(Dictionary<string, byte[]> left, Dictionary<string, byte[]> right, string name)
        => left.TryGetValue(name, out var leftValue)
            && right.TryGetValue(name, out var rightValue)
            && leftValue.SequenceEqual(rightValue);

    public static DynamicValue ClearUnknown(DynamicValue value)
    {
        var attributes = Read(value);

        if (attributes is null)
            return value;

        foreach (var (name, attribute) in attributes)
            if (IsUnknown(attribute))
                attributes[name] = Nil;

        return Write(attributes);
    }

    private static bool IsUnknown(byte[] value)
        => new MessagePackReader(value).NextMessagePackType == MessagePackType.Extension;
}
