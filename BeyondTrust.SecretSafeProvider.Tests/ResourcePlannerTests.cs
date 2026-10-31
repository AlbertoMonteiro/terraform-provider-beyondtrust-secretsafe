using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services.Resources;
using Google.Protobuf;
using MessagePack;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class ResourcePlannerTests
{
    private static readonly DynamicValue NullObject = new() { Msgpack = ByteString.CopyFrom(MessagePackCode.Nil) };

    private static FolderCredentialData Credential(string? id = null, string folderId = "folder", string title = "Title", long? ownerId = null, string? modifiedOn = null)
        => new()
        {
            Id = id,
            FolderId = folderId,
            Title = title,
            Username = "user",
            Password = "password",
            OwnerId = ownerId,
            ModifiedOn = modifiedOn
        };

    private static PlanResourceChange.Types.Response Plan(FolderCredentialData? prior, FolderCredentialData proposed, FolderCredentialData config)
        => ResourcePlanner.Plan(
            new PlanResourceChange.Types.Request
            {
                PriorState = prior is null ? NullObject : SmartSerializer.Serialize(prior),
                ProposedNewState = SmartSerializer.Serialize(proposed),
                Config = SmartSerializer.Serialize(config)
            },
            FolderCredentialData.GetSchema(),
            FolderCredentialData.ReplaceAttributes);

    private static bool IsUnknown(DynamicValue value, string name)
        => new MessagePackReader(TerraformObject.Read(value)![name]).NextMessagePackType == MessagePackType.Extension;

    [Test]
    public async Task Plan_WhenCreating_MarksComputedAttributesAsUnknown()
    {
        // Arrange
        var credential = Credential();

        // Act
        var result = Plan(prior: null, proposed: credential, config: credential);

        // Assert
        await Assert.That(IsUnknown(result.PlannedState, "id")).IsTrue();
        await Assert.That(IsUnknown(result.PlannedState, "owner_id")).IsTrue();
        await Assert.That(IsUnknown(result.PlannedState, "modified_on")).IsTrue();
        await Assert.That(IsUnknown(result.PlannedState, "title")).IsFalse();
        await Assert.That(result.RequiresReplace).IsEmpty();
    }

    [Test]
    public async Task Plan_WhenCreatingWithOwnerId_KeepsConfiguredOwner()
    {
        // Arrange
        var credential = Credential(ownerId: 5);

        // Act
        var result = Plan(prior: null, proposed: credential, config: credential);

        // Assert
        await Assert.That(IsUnknown(result.PlannedState, "owner_id")).IsFalse();
    }

    [Test]
    public async Task Plan_WhenDestroying_ReturnsNullObject()
    {
        // Arrange
        var request = new PlanResourceChange.Types.Request
        {
            PriorState = SmartSerializer.Serialize(Credential(id: "secret-id")),
            ProposedNewState = NullObject,
            Config = NullObject
        };

        // Act
        var result = ResourcePlanner.Plan(request, FolderCredentialData.GetSchema(), FolderCredentialData.ReplaceAttributes);

        // Assert
        await Assert.That(result.PlannedState).IsEqualTo(NullObject);
    }

    [Test]
    public async Task Plan_WithoutChanges_KeepsPriorValues()
    {
        // Arrange
        var prior = Credential(id: "secret-id", ownerId: 1048, modifiedOn: "2026-10-14T18:57:27Z");

        // Act
        var result = Plan(prior, proposed: prior, config: Credential());

        // Assert
        await Assert.That(result.PlannedState).IsEqualTo(SmartSerializer.Serialize(prior));
        await Assert.That(result.RequiresReplace).IsEmpty();
    }

    [Test]
    public async Task Plan_WhenUpdating_KeepsIdAndOwnerAndRefreshesComputedAttributes()
    {
        // Arrange
        var prior = Credential(id: "secret-id", ownerId: 1048, modifiedOn: "2026-10-14T18:57:27Z");
        var proposed = Credential(id: "secret-id", title: "New title", ownerId: 1048, modifiedOn: "2026-10-14T18:57:27Z");

        // Act
        var result = Plan(prior, proposed, config: Credential(title: "New title"));

        // Assert
        await Assert.That(IsUnknown(result.PlannedState, "id")).IsFalse();
        await Assert.That(IsUnknown(result.PlannedState, "owner_id")).IsFalse();
        await Assert.That(IsUnknown(result.PlannedState, "modified_on")).IsTrue();
        await Assert.That(result.RequiresReplace).IsEmpty();
    }

    [Test]
    public async Task Plan_WhenReplaceAttributeChanges_RequiresReplace()
    {
        // Arrange
        var prior = Credential(id: "secret-id");
        var proposed = Credential(id: "secret-id", folderId: "other-folder");

        // Act
        var result = Plan(prior, proposed, config: Credential(folderId: "other-folder"));

        // Assert
        await Assert.That(result.RequiresReplace).Count().IsEqualTo(1);
        await Assert.That(result.RequiresReplace[0].Steps[0].AttributeName).IsEqualTo("folder_id");
    }

    [Test]
    public async Task ClearUnknown_OnPlannedCreate_ProducesDeserializableState()
    {
        // Arrange
        var credential = Credential();
        var planned = Plan(prior: null, proposed: credential, config: credential).PlannedState;

        // Act
        var result = SmartSerializer.Deserialize<FolderCredentialData>(TerraformObject.ClearUnknown(planned));

        // Assert
        await Assert.That(result.Id).IsNull();
        await Assert.That(result.OwnerId).IsNull();
        await Assert.That(result.Title).IsEqualTo(credential.Title);
    }

    [Test]
    public async Task ClearUnknown_OnNullObject_ReturnsSameValue()
    {
        // Act
        var result = TerraformObject.ClearUnknown(NullObject);

        // Assert
        await Assert.That(result).IsEqualTo(NullObject);
    }

    [Test]
    public async Task Serialize_WritesDatesAsStringsAndOwnersAsSchemaObjects()
    {
        // Arrange
        var credential = Credential(modifiedOn: "2026-10-14T18:57:27.0833333Z");
        credential.Owners = [new OwnerInfo(1048)];

        // Act
        var serialized = SmartSerializer.Serialize(credential);
        var result = SmartSerializer.Deserialize<FolderCredentialData>(serialized);

        // Assert
        var owners = new MessagePackReader(TerraformObject.Read(serialized)!["owners"]);
        await Assert.That(ReadFirstOwnerAttribute(owners)).IsEqualTo("owner_id");
        await Assert.That(new MessagePackReader(TerraformObject.Read(serialized)!["modified_on"]).NextMessagePackType).IsEqualTo(MessagePackType.String);
        await Assert.That(result.Owners![0].OwnerId).IsEqualTo(1048);
    }

    private static string? ReadFirstOwnerAttribute(MessagePackReader reader)
    {
        reader.ReadArrayHeader();
        reader.ReadMapHeader();
        return reader.ReadString();
    }
}
