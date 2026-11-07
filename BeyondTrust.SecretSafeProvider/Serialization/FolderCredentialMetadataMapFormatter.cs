using BeyondTrust.SecretSafeProvider.Models;
using MessagePack;
using MessagePack.Formatters;

namespace BeyondTrust.SecretSafeProvider.Serialization;

/// <summary>
/// Writes the credential metadata as the map(object) declared in the schema. MessagePack builds the
/// formatter of a generic collection through reflection, which does not survive Native AOT.
/// </summary>
public sealed class FolderCredentialMetadataMapFormatter : IMessagePackFormatter<Dictionary<string, FolderCredentialMetadataEntry>?>
{
    public void Serialize(ref MessagePackWriter writer, Dictionary<string, FolderCredentialMetadataEntry>? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteMapHeader(value.Count);

        foreach (var (title, entry) in value)
        {
            writer.Write(title);
            writer.WriteMapHeader(FolderCredentialMetadataEntry.AttributeNames.Length);
            writer.Write(FolderCredentialMetadataEntry.SECRET_ID);
            writer.Write(entry.SecretId);
            writer.Write(FolderCredentialMetadataEntry.CREATED_ON);
            writer.Write(entry.CreatedOn);
            writer.Write(FolderCredentialMetadataEntry.CREATED_BY);
            writer.Write(entry.CreatedBy);
            writer.Write(FolderCredentialMetadataEntry.MODIFIED_ON);
            writer.Write(entry.ModifiedOn);
            writer.Write(FolderCredentialMetadataEntry.MODIFIED_BY);
            writer.Write(entry.ModifiedBy);
        }
    }

    public Dictionary<string, FolderCredentialMetadataEntry>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
            return null;

        var count = reader.ReadMapHeader();
        var entries = new Dictionary<string, FolderCredentialMetadataEntry>(count, StringComparer.Ordinal);

        for (var i = 0; i < count; i++)
        {
            var title = reader.ReadString()!;
            var secretId = string.Empty;
            string? createdOn = null, createdBy = null, modifiedOn = null, modifiedBy = null;
            var attributes = reader.ReadMapHeader();

            for (var j = 0; j < attributes; j++)
            {
                switch (reader.ReadString())
                {
                    case FolderCredentialMetadataEntry.SECRET_ID:   secretId = reader.ReadString() ?? string.Empty; break;
                    case FolderCredentialMetadataEntry.CREATED_ON:  createdOn = reader.ReadString(); break;
                    case FolderCredentialMetadataEntry.CREATED_BY:  createdBy = reader.ReadString(); break;
                    case FolderCredentialMetadataEntry.MODIFIED_ON: modifiedOn = reader.ReadString(); break;
                    case FolderCredentialMetadataEntry.MODIFIED_BY: modifiedBy = reader.ReadString(); break;
                    default: reader.Skip(); break;
                }
            }

            entries[title] = new FolderCredentialMetadataEntry(secretId, createdOn, createdBy, modifiedOn, modifiedBy);
        }

        return entries;
    }
}
