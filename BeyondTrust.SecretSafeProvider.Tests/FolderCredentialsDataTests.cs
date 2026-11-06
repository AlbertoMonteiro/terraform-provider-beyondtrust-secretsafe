using BeyondTrust.SecretSafeProvider.Models;
using MessagePack;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class FolderCredentialsDataTests
{
    [Test]
    public async Task MessagePack_WithCredentials_RoundTripsMapOfObjects()
    {
        // Arrange
        var sut = new FolderCredentialsData
        {
            FolderId = "7c77c6d6-a1b2-4c3d-8e9f-0123456789ab",
            Credentials = new Dictionary<string, FolderCredentialEntry>
            {
                ["db-admin"] = new("3f33f2c2-e6d3-4b28-cd01-2345678901bc", "dbadmin", "s3cret"),
                ["api-client"] = new("4a44a3d3-f7e4-4c39-de12-3456789012cd", "apiclient", "p4ss"),
            },
        };

        // Act
        var result = MessagePackSerializer.Deserialize<FolderCredentialsData>(MessagePackSerializer.Serialize(sut));

        // Assert
        await Assert.That(result.FolderId).IsEqualTo(sut.FolderId);
        await Assert.That(result.FolderName).IsNull();
        await Assert.That(result.Credentials!).Count().IsEqualTo(2);
        await Assert.That(result.Credentials!["db-admin"]).IsEqualTo(sut.Credentials["db-admin"]);
        await Assert.That(result.Credentials!["api-client"]).IsEqualTo(sut.Credentials["api-client"]);
    }

    [Test]
    public async Task MessagePack_WithEmptyCredentials_RoundTripsEmptyMap()
    {
        // Arrange
        var sut = new FolderCredentialsData { FolderName = "app", Credentials = new Dictionary<string, FolderCredentialEntry>() };

        // Act
        var result = MessagePackSerializer.Deserialize<FolderCredentialsData>(MessagePackSerializer.Serialize(sut));

        // Assert
        await Assert.That(result.Credentials).IsNotNull();
        await Assert.That(result.Credentials!).IsEmpty();
    }

    [Test]
    public async Task MessagePack_WithNullCredentials_ReadsNullLikeTerraformConfig()
    {
        // Arrange
        var sut = new FolderCredentialsData { FolderName = "app" };

        // Act
        var result = MessagePackSerializer.Deserialize<FolderCredentialsData>(MessagePackSerializer.Serialize(sut));

        // Assert
        await Assert.That(result.FolderName).IsEqualTo("app");
        await Assert.That(result.FolderId).IsNull();
        await Assert.That(result.Credentials).IsNull();
    }

    [Test]
    public async Task MessagePack_EachCredential_HasExactlyTheSchemaAttributes()
    {
        // Arrange
        var sut = new FolderCredentialsData
        {
            FolderId = "7c77c6d6-a1b2-4c3d-8e9f-0123456789ab",
            Credentials = new Dictionary<string, FolderCredentialEntry> { ["db-admin"] = new("id", "user", "pass") },
        };

        // Act
        var json = MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(sut));

        // Assert
        await Assert.That(json).Contains("""
            "credentials":{"db-admin":{"secret_id":"id","username":"user","password":"pass"}}
            """);
    }
}
