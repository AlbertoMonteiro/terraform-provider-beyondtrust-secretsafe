using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;

namespace BeyondTrust.SecretSafeProvider.Services.Resources;

public class FolderResourceHandler(
    IBeyondTrustApiFactory apiFactory,
    ProviderConfiguration configuration) : IResourceHandler
{
    public const string TYPE_NAME = "secretsafe_folder";
    public string TypeName => TYPE_NAME;

    public Schema GetSchema() => FolderResourceData.GetSchema();

    public async Task<ReadResource.Types.Response> ReadAsync(ReadResource.Types.Request request)
    {
        try
        {
            var resourceData = SmartSerializer.Deserialize<FolderResourceData>(request.CurrentState);
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
            var folderResponse = await secretSafe.GetFolder(resourceData.Id);
            await secretSafe.Signout();

            var result = new FolderResourceData
            {
                Id = folderResponse.Id,
                Name = folderResponse.Name,
                Description = folderResponse.Description,
                ParentId = folderResponse.ParentId,
                UserGroupId = folderResponse.UserGroupId
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
            var plannedState = SmartSerializer.Deserialize<FolderResourceData>(request.PlannedState);
            var resourceData = SmartSerializer.Deserialize<FolderResourceData>(request.PriorState);

            var secretSafe = apiFactory.CreateApi();

            await secretSafe.SignAppin(new KeyAndRunAs(configuration.Key, configuration.RunAs));

            FolderResponse folderResponse;

            // Create if no prior state
            if (string.IsNullOrEmpty(resourceData?.Id))
            {
                var folderRequest = new FolderRequest(
                    Name: plannedState.Name,
                    Description: plannedState.Description,
                    ParentId: plannedState.ParentId,
                    UserGroupId: plannedState.UserGroupId);

                folderResponse = await secretSafe.CreateFolder(folderRequest);
            }
            else
            {
                // Update if prior state exists
                var folderRequest = new FolderRequest(
                    Name: plannedState.Name,
                    Description: plannedState.Description,
                    ParentId: plannedState.ParentId,
                    UserGroupId: plannedState.UserGroupId);

                folderResponse = await secretSafe.UpdateFolder(resourceData.Id, folderRequest);
            }

            // Handle delete if planned state is empty (Terraform delete)
            if (request.PlannedState.Msgpack.IsEmpty && request.PlannedState.Json.IsEmpty)
            {
                if (!string.IsNullOrEmpty(resourceData?.Id))
                {
                    var deleteResponse = await secretSafe.DeleteFolder(resourceData.Id);
                    if (deleteResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        await secretSafe.Signout();
                        return new ApplyResourceChange.Types.Response
                        {
                            Diagnostics =
                            {
                                new Diagnostic()
                                {
                                    Severity = Diagnostic.Types.Severity.Error,
                                    Summary = "Cannot Delete Folder",
                                    Detail = "The folder contains secrets. Please delete all secrets before deleting the folder."
                                }
                            }
                        };
                    }
                }

                await secretSafe.Signout();
                return new ApplyResourceChange.Types.Response
                {
                    NewState = new DynamicValue()
                };
            }

            await secretSafe.Signout();

            var result = new FolderResourceData
            {
                Id = folderResponse.Id,
                Name = folderResponse.Name,
                Description = folderResponse.Description,
                ParentId = folderResponse.ParentId,
                UserGroupId = folderResponse.UserGroupId
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
