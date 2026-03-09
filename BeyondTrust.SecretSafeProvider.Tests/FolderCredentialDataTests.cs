using BeyondTrust.SecretSafeProvider.Models;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class FolderCredentialDataTests
{
    [Test]
    public async Task GetSchema_ReturnsSchemaWithVersionOne()
    {
        // Act
        var schema = FolderCredentialData.GetSchema();

        // Assert
        await Assert.That(schema.Version).IsEqualTo(1);
    }

    [Test]
    public async Task GetSchema_ReturnsSchemaWithEightAttributes()
    {
        // Act
        var schema = FolderCredentialData.GetSchema();

        // Assert
        await Assert.That(schema.Block.Attributes).Count().IsEqualTo(8);
    }

    [Test]
    public async Task GetSchema_IdAttribute_IsComputedString()
    {
        // Act
        var schema = FolderCredentialData.GetSchema();
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
        var schema = FolderCredentialData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "folder_id");

        // Assert
        await Assert.That(attr.Required).IsTrue();
        await Assert.That(attr.Computed).IsFalse();
        await Assert.That(attr.Sensitive).IsFalse();
    }

    [Test]
    public async Task GetSchema_TitleAttribute_IsRequiredString()
    {
        // Act
        var schema = FolderCredentialData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "title");

        // Assert
        await Assert.That(attr.Required).IsTrue();
        await Assert.That(attr.Computed).IsFalse();
    }

    [Test]
    public async Task GetSchema_UsernameAttribute_IsRequiredString()
    {
        // Act
        var schema = FolderCredentialData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "username");

        // Assert
        await Assert.That(attr.Required).IsTrue();
        await Assert.That(attr.Computed).IsFalse();
    }

    [Test]
    public async Task GetSchema_PasswordAttribute_IsRequiredSensitiveString()
    {
        // Act
        var schema = FolderCredentialData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "password");

        // Assert
        await Assert.That(attr.Required).IsTrue();
        await Assert.That(attr.Sensitive).IsTrue();
    }

    [Test]
    public async Task GetSchema_OwnerIdAttribute_IsRequiredNumber()
    {
        // Act
        var schema = FolderCredentialData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "owner_id");

        // Assert
        await Assert.That(attr.Required).IsTrue();
        await Assert.That(attr.Computed).IsFalse();
    }
}
