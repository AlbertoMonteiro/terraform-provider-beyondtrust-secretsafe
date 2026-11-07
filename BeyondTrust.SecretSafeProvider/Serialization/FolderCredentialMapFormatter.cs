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
            writer.WriteMapHeader(FolderCredentialEntry.AttributeNames.Length);
            writer.Write(FolderCredentialEntry.SECRET_ID);
            writer.Write(credential.SecretId);
            writer.Write(FolderCredentialEntry.USERNAME);
            writer.Write(credential.Username);
            writer.Write(FolderCredentialEntry.PASSWORD);
            writer.Write(credential.Password);
            writer.Write(FolderCredentialEntry.CREATED_ON);
            writer.Write(credential.CreatedOn);
            writer.Write(FolderCredentialEntry.CREATED_BY);
            writer.Write(credential.CreatedBy);
            writer.Write(FolderCredentialEntry.MODIFIED_ON);
            writer.Write(credential.ModifiedOn);
            writer.Write(FolderCredentialEntry.MODIFIED_BY);
            writer.Write(credential.ModifiedBy);
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
            string? createdOn = null, createdBy = null, modifiedOn = null, modifiedBy = null;
            var attributes = reader.ReadMapHeader();

            for (var j = 0; j < attributes; j++)
            {
                switch (reader.ReadString())
                {
                    case FolderCredentialEntry.SECRET_ID:   secretId = reader.ReadString() ?? string.Empty; break;
                    case FolderCredentialEntry.USERNAME:    username = reader.ReadString() ?? string.Empty; break;
                    case FolderCredentialEntry.PASSWORD:    password = reader.ReadString() ?? string.Empty; break;
                    case FolderCredentialEntry.CREATED_ON:  createdOn = reader.ReadString(); break;
                    case FolderCredentialEntry.CREATED_BY:  createdBy = reader.ReadString(); break;
                    case FolderCredentialEntry.MODIFIED_ON: modifiedOn = reader.ReadString(); break;
                    case FolderCredentialEntry.MODIFIED_BY: modifiedBy = reader.ReadString(); break;
                    default: reader.Skip(); break;
                }
            }

            credentials[title] = new FolderCredentialEntry(secretId, username, password, createdOn, createdBy, modifiedOn, modifiedBy);
        }

        return credentials;
    }
}
