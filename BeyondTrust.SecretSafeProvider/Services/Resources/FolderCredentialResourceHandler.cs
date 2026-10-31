using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;

namespace BeyondTrust.SecretSafeProvider.Services.Resources;

public class FolderCredentialResourceHandler(
    IBeyondTrustApiFactory apiFactory,
    ProviderConfiguration configuration) : IResourceHandler
{
    public const string TYPE_NAME = "secretsafe_folder_credential";
    public string TypeName => TYPE_NAME;
    public IReadOnlySet<string> ReplaceAttributes => FolderCredentialData.ReplaceAttributes;

    public Schema GetSchema() => FolderCredentialData.GetSchema();

    public async Task<ReadResource.Types.Response> ReadAsync(ReadResource.Types.Request request)
    {
        try
        {
            var resourceData = SmartSerializer.Deserialize<FolderCredentialData>(request.CurrentState);
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

            await secretSafe.OpenSession(configuration);
            var secretResponse = await secretSafe.GetSecret(Guid.Parse(resourceData.Id));
            await secretSafe.CloseSession(configuration);

            var result = new FolderCredentialData
            {
                Id = secretResponse.Id,
                FolderId = secretResponse.FolderId ?? resourceData.FolderId,
                Title = secretResponse.Title ?? string.Empty,
                Username = secretResponse.Username ?? string.Empty,
                Password = secretResponse.Password ?? string.Empty,
                OwnerId = secretResponse.OwnerId,
                Description = string.IsNullOrEmpty(secretResponse.Description) ? resourceData.Description : secretResponse.Description,
                Owners = secretResponse.Owners,
                CreatedOn = secretResponse.CreatedOn,
                CreatedBy = secretResponse.CreatedBy,
                ModifiedOn = secretResponse.ModifiedOn,
                ModifiedBy = secretResponse.ModifiedBy,
                Owner = secretResponse.Owner,
                Folder = secretResponse.Folder,
                FolderPath = secretResponse.FolderPath,
                OwnerType = secretResponse.OwnerType,
                Notes = secretResponse.Notes
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
            var plannedState = SmartSerializer.Deserialize<FolderCredentialData>(request.PlannedState);
            var resourceData = SmartSerializer.Deserialize<FolderCredentialData>(request.PriorState);

            var secretSafe = apiFactory.CreateApi();

            var session = await secretSafe.OpenSession(configuration);

            // Terraform plans a null object when destroying
            if (plannedState is null)
            {
                await secretSafe.DeleteSecretIfExists(resourceData.Id);
                await secretSafe.CloseSession(configuration);

                return new ApplyResourceChange.Types.Response { NewState = request.PlannedState };
            }

            var ownerId = await apiFactory.ResolveOwnerId(session, plannedState.OwnerId);

            SecretResponse? secretResponse = null;

            // Create if no prior state
            if (string.IsNullOrEmpty(resourceData?.Id))
            {
                var request_body = new CreateSecretCredentialRequest(
                    Title: plannedState.Title,
                    Description: plannedState.Description,
                    Username: plannedState.Username,
                    Password: plannedState.Password,
                    OwnerId: ownerId,
                    Owners: null);

                secretResponse = await secretSafe.CreateCredentialSecret(plannedState.FolderId, request_body);
            }
            else
            {
                // Update if prior state exists
                var request_body = new CreateSecretCredentialRequest(
                    Title: plannedState.Title,
                    Description: plannedState.Description,
                    Username: plannedState.Username,
                    Password: plannedState.Password,
                    OwnerId: ownerId,
                    Owners: null,
                    FolderId: plannedState.FolderId);

                // The API answers 204, so the computed values must be read back
                await secretSafe.UpdateCredentialSecret(resourceData.Id, request_body);
                var updated = await secretSafe.GetSecret(Guid.Parse(resourceData.Id));

                secretResponse = new SecretResponse(
                    Id: resourceData.Id,
                    OwnerId: updated.OwnerId,
                    CreatedOn: updated.CreatedOn,
                    CreatedBy: updated.CreatedBy,
                    ModifiedOn: updated.ModifiedOn,
                    ModifiedBy: updated.ModifiedBy,
                    Owner: updated.Owner,
                    Folder: updated.Folder,
                    FolderPath: updated.FolderPath,
                    Owners: updated.Owners,
                    OwnerType: updated.OwnerType,
                    Notes: updated.Notes);
            }

            await secretSafe.CloseSession(configuration);

            var result = new FolderCredentialData
            {
                Id = secretResponse.Id,
                FolderId = plannedState.FolderId,
                Title = plannedState.Title,
                Description = plannedState.Description,
                Username = plannedState.Username,
                Password = plannedState.Password,
                OwnerId = plannedState.OwnerId ?? secretResponse.OwnerId,
                Owners = secretResponse.Owners,
                CreatedOn = secretResponse.CreatedOn,
                CreatedBy = secretResponse.CreatedBy,
                ModifiedOn = secretResponse.ModifiedOn,
                ModifiedBy = secretResponse.ModifiedBy,
                Owner = secretResponse.Owner,
                Folder = secretResponse.Folder,
                FolderPath = secretResponse.FolderPath,
                OwnerType = secretResponse.OwnerType,
                Notes = secretResponse.Notes
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
