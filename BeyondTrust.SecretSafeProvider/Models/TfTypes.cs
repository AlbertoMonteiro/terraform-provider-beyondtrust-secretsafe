using Google.Protobuf;

namespace BeyondTrust.SecretSafeProvider.Models;

public static class TfTypes
{
    public static readonly ByteString String = ByteString.CopyFromUtf8("\"string\"");
    public static readonly ByteString Number = ByteString.CopyFromUtf8("\"number\"");

    public static ByteString List(ByteString innerType) =>
        ByteString.CopyFromUtf8($"[{innerType.ToStringUtf8()}]");

    public static ByteString Object(Dictionary<string, ByteString> attributes)
    {
        var attrs = string.Join(",", attributes.Select(a => $"\"{a.Key}\":{a.Value.ToStringUtf8()}"));
        return ByteString.CopyFromUtf8($"{{{attrs}}}");
    }
}