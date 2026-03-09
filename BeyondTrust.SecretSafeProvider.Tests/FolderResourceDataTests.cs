using BeyondTrust.SecretSafeProvider.Models;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class FolderResourceDataTests
{
    [Test]
    public async Task GetSchema_ReturnsSchemaWithVersionOne()
    {
        // Act
        var schema = FolderResourceData.GetSchema();

        // Assert
        await Assert.That(schema.Version).IsEqualTo(1);
    }

    [Test]
    public async Task GetSchema_ReturnsSchemaWithFiveAttributes()
    {
        // Act
        var schema = FolderResourceData.GetSchema();

        // Assert
        await Assert.That(schema.Block.Attributes).Count().IsEqualTo(5);
    }

    [Test]
    public async Task GetSchema_IdAttribute_IsComputedString()
    {
        // Act
        var schema = FolderResourceData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "id");

        // Assert
        await Assert.That(attr.Computed).IsTrue();
        await Assert.That(attr.Required).IsFalse();
    }

    [Test]
    public async Task GetSchema_NameAttribute_IsRequiredString()
    {
        // Act
        var schema = FolderResourceData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "name");

        // Assert
        await Assert.That(attr.Required).IsTrue();
        await Assert.That(attr.Computed).IsFalse();
    }

    [Test]
    public async Task GetSchema_UserGroupIdAttribute_IsRequiredNumber()
    {
        // Act
        var schema = FolderResourceData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "user_group_id");

        // Assert
        await Assert.That(attr.Required).IsTrue();
        await Assert.That(attr.Computed).IsFalse();
    }
}
