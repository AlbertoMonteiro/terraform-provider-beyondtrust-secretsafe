using BeyondTrust.SecretSafeProvider.Models;
using Refit;

namespace BeyondTrust.SecretSafeProvider.Services;

public interface IBeyondTrustSecretSafe
{
    private const string V3 = "/public/v3";

    [Post($"{V3}/Auth/SignAppin")]
    public Task<SignAppinResponse> SignAppin([Authorize("PS-Auth")] KeyAndRunAs keyAndRunAs);

    [Get($"{V3}/Secrets-Safe/Secrets/{{secretId}}")]
    public Task<SecretValue> GetSecret(Guid secretId);

    [Get($"{V3}/Secrets-Safe/Secrets/{{secretId}}/file/download")]
    public Task<HttpResponseMessage> DownloadSecret(Guid secretId);

    [Post($"{V3}/Auth/Signout")]
    public Task<ApiResponse<HttpResponseMessage>> Signout();
}
