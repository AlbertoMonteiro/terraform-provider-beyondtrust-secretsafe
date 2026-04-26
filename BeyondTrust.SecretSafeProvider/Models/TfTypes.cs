using Google.Protobuf;

namespace BeyondTrust.SecretSafeProvider.Models;

// Terraform plugin protocol v5 expects each `Schema.Attribute.Type` to be a
// JSON-encoded cty type. Primitive types are bare strings (e.g. "string"),
// while complex types are 2-element arrays whose first item is the kind name.
// See https://github.com/zclconf/go-cty/blob/main/docs/json.md
public static class TfTypes
{
    public static readonly ByteString String = ByteString.CopyFromUtf8("\"string\"");
    public static readonly ByteString Number = ByteString.CopyFromUtf8("\"number\"");
    public static readonly ByteString Bool = ByteString.CopyFromUtf8("\"bool\"");

    public static ByteString List(ByteString elementType) =>
        ByteString.CopyFromUtf8($"[\"list\",{elementType.ToStringUtf8()}]");

    public static ByteString Set(ByteString elementType) =>
        ByteString.CopyFromUtf8($"[\"set\",{elementType.ToStringUtf8()}]");

    public static ByteString Map(ByteString elementType) =>
        ByteString.CopyFromUtf8($"[\"map\",{elementType.ToStringUtf8()}]");

    public static ByteString Object(Dictionary<string, ByteString> attributes)
    {
        var attrs = string.Join(",", attributes.Select(a => $"\"{a.Key}\":{a.Value.ToStringUtf8()}"));
        return ByteString.CopyFromUtf8($"[\"object\",{{{attrs}}}]");
    }
}