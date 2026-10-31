using System.Text.Json;
using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services;
using BeyondTrust.SecretSafeProvider.Services.Resources;
using Imposter.Abstractions;

namespace BeyondTrust.SecretSafeProvider.Tests;

/// <summary>Payload shapes observed on a real BeyondTrust instance.</summary>
public class ApiPayloadTests
{
    private const string FOLDER_JSON = """
        {
            "Id": "06b17a29-adf3-4b06-8972-08df257f0006",
            "Name": "folder",
            "Description": "Safe Provisionado",
            "ParentId": null,
            "UserGroupId": null,
            "UserId": null,
            "FolderOwnerId": null
        }
        """;

    private const string SECRET_JSON = """
        {
            "Username": "user",
            "Password": "password",
            "Group": null,
            "FileName": null,
            "FileHash": null,
            "Text": null,
            "SecretType": "Credential",
            "Id": "b565ceaa-2481-4b34-6e9c-08df257f009e",
            "Title": "title",
            "Description": "",
            "OwnerId": 1048,
            "GroupId": null,
            "FolderId": "06b17a29-adf3-4b06-8972-08df257f0006",
            "CreatedOn": "2026-10-14T18:57:27.0833333Z",
            "CreatedBy": "app",
            "ModifiedOn": "2026-10-14T18:57:27.0833333Z",
            "ModifiedBy": "app",
            "Owner": "app",
            "Folder": "folder",
            "FolderPath": "folder",
            "Owners": [
                { "OwnerId": 1048, "GroupId": null, "UserId": 1048, "Owner": "app", "Name": "app", "Email": null }
            ],
            "OwnerType": "User",
            "Notes": "notes",
            "Urls": []
        }
        """;

    [Test]
    public async Task FolderResponse_WithNullUserGroupId_Deserializes()
    {
        // Act
        var folder = JsonSerializer.Deserialize(FOLDER_JSON, BeyondTrust.SecretSafeProvider.Serialization.Json.Default.FolderResponse);

        // Assert
        await Assert.That(folder!.Id).IsEqualTo("06b17a29-adf3-4b06-8972-08df257f0006");
        await Assert.That(folder.UserGroupId).IsNull();
    }

    [Test]
    public async Task SecretValue_WithFullPayload_Deserializes()
    {
        // Act
        var secret = JsonSerializer.Deserialize(SECRET_JSON, BeyondTrust.SecretSafeProvider.Serialization.Json.Default.SecretValue);

        // Assert
        await Assert.That(secret!.Username).IsEqualTo("user");
        await Assert.That(secret.Password).IsEqualTo("password");
        await Assert.That(secret.OwnerId).IsEqualTo(1048);
        await Assert.That(secret.Owners![0].OwnerId).IsEqualTo(1048);
    }

    [Test]
    public async Task ReadAsync_WhenCredentialHasEmptyDescription_KeepsNullFromState()
    {
        // Arrange
        var secretId = Guid.NewGuid();
        var secretSafe = IBeyondTrustSecretSafe.Imposter();
        var factory = IBeyondTrustApiFactory.Imposter();
        factory.CreateApi().Returns(secretSafe.Instance());
        var configuration = new ProviderConfiguration { BaseUrl = "", ClientId = "id", ClientSecret = "secret" };
        var sut = new FolderCredentialResourceHandler(factory.Instance(), configuration);

        var state = new FolderCredentialData { Id = secretId.ToString(), FolderId = "folder", Title = "title", Username = "user", Password = "password" };
        var request = new ReadResource.Types.Request { CurrentState = SmartSerializer.Serialize(state) };
        secretSafe.GetSecret(secretId).ReturnsAsync(new SecretValue(Id: secretId.ToString(), Description: ""));

        // Act
        var result = await sut.ReadAsync(request);
        var resultData = SmartSerializer.Deserialize<FolderCredentialData>(result.NewState);

        // Assert
        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(resultData.Description).IsNull();
    }

    [Test]
    public async Task ReadAsync_WhenFolderHasNoUserGroupId_KeepsValueFromState()
    {
        // Arrange
        var secretSafe = IBeyondTrustSecretSafe.Imposter();
        var factory = IBeyondTrustApiFactory.Imposter();
        factory.CreateApi().Returns(secretSafe.Instance());
        var configuration = new ProviderConfiguration { BaseUrl = "", ClientId = "id", ClientSecret = "secret" };
        var sut = new FolderResourceHandler(factory.Instance(), configuration);

        var state = new FolderResourceData { Id = "folder-id", Name = "folder", OwnerId = 5, UserGroupId = 7 };
        var request = new ReadResource.Types.Request { CurrentState = SmartSerializer.Serialize(state) };
        secretSafe.GetFolder("folder-id").ReturnsAsync(new FolderResponse("folder-id", "folder", null, null, null));

        // Act
        var result = await sut.ReadAsync(request);
        var resultData = SmartSerializer.Deserialize<FolderResourceData>(result.NewState);

        // Assert
        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(resultData.UserGroupId).IsEqualTo(7);
    }
}
