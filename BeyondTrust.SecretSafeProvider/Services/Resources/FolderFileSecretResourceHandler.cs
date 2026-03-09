using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using Refit;

namespace BeyondTrust.SecretSafeProvider.Services.Resources;

public class FolderFileSecretResourceHandler(
    IBeyondTrustApiFactory apiFactory,
    ProviderConfiguration configuration) : IResourceHandler
{
    public const string TYPE_NAME = "secretsafe_folder_file";
    public string TypeName => TYPE_NAME;

    public Schema GetSchema() => FolderFileSecretData.GetSchema();

    public async Task<ReadResource.Types.Response> ReadAsync(ReadResource.Types.Request request)
    {
        try
        {
            var resourceData = SmartSerializer.Deserialize<FolderFileSecretData>(request.CurrentState);
            if (string.IsNullOrEmpty(resourceData.Id))
            {
                return new ReadResource.Types.Response
                {
                    Diagnostics =
                    {
                        new Diagnostic()
                        {
                            Severity = Diagnostic.Types.Severity.Error,
                            Summary = "Invalid State",
                            Detail = "Resource ID is missing from state"
                        }
                    }
                };
            }

            var secretSafe = apiFactory.CreateApi();

            await secretSafe.SignAppin(new KeyAndRunAs(configuration.Key, configuration.RunAs));
            var secretResponse = await secretSafe.GetSecret(Guid.Parse(resourceData.Id));
            await secretSafe.Signout();

            var result = new FolderFileSecretData
            {
                Id = resourceData.Id,
                FolderId = resourceData.FolderId,
                Title = resourceData.Title,
                Description = resourceData.Description,
                FileName = resourceData.FileName,
                FileContentBase64 = resourceData.FileContentBase64,
                OwnerId = resourceData.OwnerId,
                Owners = resourceData.Owners
            };

            return new ReadResource.Types.Response
            {
                NewState = SmartSerializer.Serialize(result)
            };
        }
        catch (Exception ex)
        {
            return new ReadResource.Types.Response
            {
                Diagnostics =
                {
                    new Diagnostic()
                    {
                        Severity = Diagnostic.Types.Severity.Error,
                        Summary = "Error",
                        Detail = ex.Message
                    }
                }
            };
        }
    }

    public async Task<ApplyResourceChange.Types.Response> ApplyAsync(ApplyResourceChange.Types.Request request)
    {
        try
        {
            var plannedState = SmartSerializer.Deserialize<FolderFileSecretData>(request.PlannedState);
            var resourceData = SmartSerializer.Deserialize<FolderFileSecretData>(request.PriorState);

            var secretSafe = apiFactory.CreateApi();

            var signAppinResponse = await secretSafe.SignAppin(new KeyAndRunAs(configuration.Key, configuration.RunAs));
            var ownerId = signAppinResponse.UserId;

            string resourceId;

            // Create if no prior state
            if (string.IsNullOrEmpty(resourceData?.Id))
            {
                // Decode base64 to bytes for file upload
                byte[] fileBytes = Convert.FromBase64String(plannedState.FileContentBase64);

                var metadata = new CreateSecretFileRequest(
                    Title: plannedState.Title,
                    Description: plannedState.Description,
                    FileName: plannedState.FileName,
                    FileContent: plannedState.FileContentBase64,
                    OwnerId: ownerId,
                    Owners: null);

                using var stream = new MemoryStream(fileBytes);
                var filePart = new StreamPart(stream, plannedState.FileName, "application/octet-stream");

                var secretResponse = await secretSafe.CreateFileSecret(plannedState.FolderId, metadata, filePart);
                resourceId = secretResponse.Id;
            }
            else
            {
                // For file updates, would need to implement similar logic
                // For now, just re-create with the same ID
                byte[] fileBytes = Convert.FromBase64String(plannedState.FileContentBase64);

                var metadata = new CreateSecretFileRequest(
                    Title: plannedState.Title,
                    Description: plannedState.Description,
                    FileName: plannedState.FileName,
                    FileContent: plannedState.FileContentBase64,
                    OwnerId: ownerId,
                    Owners: null);

                using var stream = new MemoryStream(fileBytes);
                var filePart = new StreamPart(stream, plannedState.FileName, "application/octet-stream");

                var secretResponse = await secretSafe.CreateFileSecret(plannedState.FolderId, metadata, filePart);
                resourceId = secretResponse.Id;
            }

            await secretSafe.Signout();

            var result = new FolderFileSecretData
            {
                Id = resourceId,
                FolderId = plannedState.FolderId,
                Title = plannedState.Title,
                Description = plannedState.Description,
                FileName = plannedState.FileName,
                FileContentBase64 = plannedState.FileContentBase64,
                OwnerId = ownerId,
                Owners = null
            };

            return new ApplyResourceChange.Types.Response
            {
                NewState = SmartSerializer.Serialize(result)
            };
        }
        catch (Exception ex)
        {
            return new ApplyResourceChange.Types.Response
            {
                Diagnostics =
                {
                    new Diagnostic()
                    {
                        Severity = Diagnostic.Types.Severity.Error,
                        Summary = "Error",
                        Detail = ex.Message
                    }
                }
            };
        }
    }
}
