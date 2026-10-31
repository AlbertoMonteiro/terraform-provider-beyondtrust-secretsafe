using BeyondTrust.SecretSafeProvider.Models;
using MessagePack;
using MessagePack.Formatters;

namespace BeyondTrust.SecretSafeProvider.Serialization;

/// <summary>
/// Writes owners as the list(object) declared in the schema. MessagePack builds the formatter of a
/// generic collection through reflection, which does not survive Native AOT.
/// </summary>
public sealed class OwnerInfoListFormatter : IMessagePackFormatter<IList<OwnerInfo>?>
{
    private const string OWNER_ID = "owner_id";

    public void Serialize(ref MessagePackWriter writer, IList<OwnerInfo>? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteArrayHeader(value.Count);

        foreach (var owner in value)
        {
            writer.WriteMapHeader(1);
            writer.Write(OWNER_ID);
            writer.Write(owner.OwnerId);
        }
    }

    public IList<OwnerInfo>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
            return null;

        var count = reader.ReadArrayHeader();
        var owners = new List<OwnerInfo>(count);

        for (var i = 0; i < count; i++)
        {
            long ownerId = default;
            var attributes = reader.ReadMapHeader();

            for (var j = 0; j < attributes; j++)
            {
                if (reader.ReadString() == OWNER_ID)
                    ownerId = reader.ReadInt64();
                else
                    reader.Skip();
            }

            owners.Add(new OwnerInfo(ownerId));
        }

        return owners;
    }
}
