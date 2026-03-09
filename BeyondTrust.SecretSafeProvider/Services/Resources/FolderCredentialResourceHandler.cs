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

            await secretSafe.SignAppin(new KeyAndRunAs(configuration.Key, configuration.RunAs, configuration.Pwd));
            var secretResponse = await secretSafe.GetSecret(Guid.Parse(resourceData.Id));
            await secretSafe.Signout();

            var result = new FolderCredentialData
            {
                Id = resourceData.Id,
                FolderId = resourceData.FolderId,
                Title = secretResponse.Username ?? string.Empty,
                Username = secretResponse.Username ?? string.Empty,
                Password = secretResponse.Password ?? string.Empty,
                OwnerId = resourceData.OwnerId,
                Description = resourceData.Description,
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
            var plannedState = SmartSerializer.Deserialize<FolderCredentialData>(request.PlannedState);
            var resourceData = SmartSerializer.Deserialize<FolderCredentialData>(request.PriorState);

            var secretSafe = apiFactory.CreateApi();

            var signAppinResponse = await secretSafe.SignAppin(new KeyAndRunAs(configuration.Key, configuration.RunAs, configuration.Pwd));
            var ownerId = signAppinResponse.UserId;

            string resourceId;

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

                var secretResponse = await secretSafe.CreateCredentialSecret(plannedState.FolderId, request_body);
                resourceId = secretResponse.Id;
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
                    Owners: null);

                var secretResponse = await secretSafe.UpdateCredentialSecret(resourceData.Id, request_body);
                resourceId = secretResponse.Id;
            }

            await secretSafe.Signout();

            var result = new FolderCredentialData
            {
                Id = resourceId,
                FolderId = plannedState.FolderId,
                Title = plannedState.Title,
                Description = plannedState.Description,
                Username = plannedState.Username,
                Password = plannedState.Password,
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
