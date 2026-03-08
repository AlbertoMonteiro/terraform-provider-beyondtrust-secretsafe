using BeyondTrust.SecretSafeProvider.Models;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class FileDownloadDataTests
{
    [Test]
    public async Task GetSchema_ReturnsSchemaWithVersionOne()
    {
        // Act
        var schema = FileDownloadData.GetSchema();

        // Assert
        await Assert.That(schema.Version).IsEqualTo(1);
    }

    [Test]
    public async Task GetSchema_ReturnsSchemaWithThreeAttributes()
    {
        // Act
        var schema = FileDownloadData.GetSchema();

        // Assert
        await Assert.That(schema.Block.Attributes).Count().IsEqualTo(3);
    }

    [Test]
    public async Task GetSchema_SecretIdAttribute_IsRequiredString()
    {
        // Act
        var schema = FileDownloadData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "secret_id");

        // Assert
        await Assert.That(attr.Required).IsTrue();
        await Assert.That(attr.Computed).IsFalse();
        await Assert.That(attr.Sensitive).IsFalse();
    }

    [Test]
    public async Task GetSchema_FileNameAttribute_IsComputedString()
    {
        // Act
        var schema = FileDownloadData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "file_name");

        // Assert
        await Assert.That(attr.Computed).IsTrue();
        await Assert.That(attr.Required).IsFalse();
        await Assert.That(attr.Sensitive).IsFalse();
    }

    [Test]
    public async Task GetSchema_FileContentBase64Attribute_IsComputedSensitiveString()
    {
        // Act
        var schema = FileDownloadData.GetSchema();
        var attr = schema.Block.Attributes.Single(a => a.Name == "file_content_base64");

        // Assert
        await Assert.That(attr.Computed).IsTrue();
        await Assert.That(attr.Sensitive).IsTrue();
        await Assert.That(attr.Required).IsFalse();
    }
}
