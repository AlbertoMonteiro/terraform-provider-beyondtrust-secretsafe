using BeyondTrust.SecretSafeProvider.Models;
using MessagePack;
using MessagePack.Formatters;

namespace BeyondTrust.SecretSafeProvider.Serialization;

/// <summary>
/// Writes credentials as the map(object) declared in the schema. MessagePack builds the formatter of a
/// generic collection through reflection, which does not survive Native AOT.
/// </summary>
public sealed class FolderCredentialMapFormatter : IMessagePackFormatter<IDictionary<string, FolderCredentialEntry>?>
{
    public void Serialize(ref MessagePackWriter writer, IDictionary<string, FolderCredentialEntry>? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteMapHeader(value.Count);

        foreach (var (title, credential) in value)
        {
            writer.Write(title);
            writer.WriteMapHeader(3);
            writer.Write(FolderCredentialEntry.SECRET_ID);
            writer.Write(credential.SecretId);
            writer.Write(FolderCredentialEntry.USERNAME);
            writer.Write(credential.Username);
            writer.Write(FolderCredentialEntry.PASSWORD);
            writer.Write(credential.Password);
        }
    }

    public IDictionary<string, FolderCredentialEntry>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
            return null;

        var count = reader.ReadMapHeader();
        var credentials = new Dictionary<string, FolderCredentialEntry>(count, StringComparer.Ordinal);

        for (var i = 0; i < count; i++)
        {
            var title = reader.ReadString()!;
            string secretId = string.Empty, username = string.Empty, password = string.Empty;
            var attributes = reader.ReadMapHeader();

            for (var j = 0; j < attributes; j++)
            {
                switch (reader.ReadString())
                {
                    case FolderCredentialEntry.SECRET_ID: secretId = reader.ReadString() ?? string.Empty; break;
                    case FolderCredentialEntry.USERNAME:  username = reader.ReadString() ?? string.Empty; break;
                    case FolderCredentialEntry.PASSWORD:  password = reader.ReadString() ?? string.Empty; break;
                    default: reader.Skip(); break;
                }
            }

            credentials[title] = new FolderCredentialEntry(secretId, username, password);
        }

        return credentials;
    }
}
