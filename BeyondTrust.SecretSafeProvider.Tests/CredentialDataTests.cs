using BeyondTrust.SecretSafeProvider.Models;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class CredentialDataTests
{
    [Test]
    public async Task GetSchema_ReturnsSchemaWithVersionOne()
    {
        // Act
        var schema = CredentialData.GetSchema();

        // Assert
        await Assert.That(schema.Version).IsEqualTo(1);
    }

    [Test]
    public async Task GetSchema_ReturnsCredentialAndMetadataAttributes()
    {
        // Act
        var schema = CredentialData.GetSchema();

        // Assert
        await Assert.That(schema.Block.Attributes).Count().IsEqualTo(15);
    }

    [Test]
    public async Task GetSchema_SecretIdAttribute_IsRequiredString()
    {
        // Act
        var schema = CredentialData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "secret_id");

        // Assert
        await Assert.That(attr.Required).IsTrue();
        await Assert.That(attr.Computed).IsFalse();
        await Assert.That(attr.Sensitive).IsFalse();
    }

    [Test]
    public async Task GetSchema_UsernameAttribute_IsComputedString()
    {
        // Act
        var schema = CredentialData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "username");

        // Assert
        await Assert.That(attr.Computed).IsTrue();
        await Assert.That(attr.Required).IsFalse();
        await Assert.That(attr.Sensitive).IsFalse();
    }

    [Test]
    public async Task GetSchema_PasswordAttribute_IsComputedSensitiveString()
    {
        // Act
        var schema = CredentialData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "password");

        // Assert
        await Assert.That(attr.Computed).IsTrue();
        await Assert.That(attr.Sensitive).IsTrue();
        await Assert.That(attr.Required).IsFalse();
    }
}
