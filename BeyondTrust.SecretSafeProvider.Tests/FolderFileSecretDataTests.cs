using BeyondTrust.SecretSafeProvider.Models;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class FolderFileSecretDataTests
{
    [Test]
    public async Task GetSchema_ReturnsSchemaWithVersionOne()
    {
        // Act
        var schema = FolderFileSecretData.GetSchema();

        // Assert
        await Assert.That(schema.Version).IsEqualTo(1);
    }

    [Test]
    public async Task GetSchema_ReturnsSchemaWithEightAttributes()
    {
        // Act
        var schema = FolderFileSecretData.GetSchema();

        // Assert
        await Assert.That(schema.Block.Attributes).Count().IsEqualTo(8);
    }

    [Test]
    public async Task GetSchema_IdAttribute_IsComputedString()
    {
        // Act
        var schema = FolderFileSecretData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "id");

        // Assert
        await Assert.That(attr.Computed).IsTrue();
        await Assert.That(attr.Required).IsFalse();
        await Assert.That(attr.Sensitive).IsFalse();
    }

    [Test]
    public async Task GetSchema_FolderIdAttribute_IsRequiredString()
    {
        // Act
        var schema = FolderFileSecretData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "folder_id");

        // Assert
        await Assert.That(attr.Required).IsTrue();
        await Assert.That(attr.Computed).IsFalse();
    }

    [Test]
    public async Task GetSchema_TitleAttribute_IsRequiredString()
    {
        // Act
        var schema = FolderFileSecretData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "title");

        // Assert
        await Assert.That(attr.Required).IsTrue();
        await Assert.That(attr.Computed).IsFalse();
    }

    [Test]
    public async Task GetSchema_FileNameAttribute_IsRequiredString()
    {
        // Act
        var schema = FolderFileSecretData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "file_name");

        // Assert
        await Assert.That(attr.Required).IsTrue();
        await Assert.That(attr.Computed).IsFalse();
    }

    [Test]
    public async Task GetSchema_FileContentBase64Attribute_IsRequiredSensitiveString()
    {
        // Act
        var schema = FolderFileSecretData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "file_content_base64");

        // Assert
        await Assert.That(attr.Required).IsTrue();
        await Assert.That(attr.Sensitive).IsTrue();
    }

    [Test]
    public async Task GetSchema_OwnerIdAttribute_IsComputedNumber()
    {
        // Act
        var schema = FolderFileSecretData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "owner_id");

        // Assert
        await Assert.That(attr.Computed).IsTrue();
        await Assert.That(attr.Required).IsFalse();
    }
}
