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

    [Post($"{V3}/Secrets-Safe/Folders/{{folderId}}/secrets")]
    public Task<SecretResponse> CreateCredentialSecret(string folderId, [Body] CreateSecretCredentialRequest request);

    [Put($"{V3}/Secrets-Safe/Secrets/{{secretId}}")]
    public Task<SecretResponse> UpdateCredentialSecret(string secretId, [Body] CreateSecretCredentialRequest request);

    [Multipart]
    [Post($"{V3}/Secrets-Safe/Folders/{{folderId}}/secrets/file")]
    public Task<SecretResponse> CreateFileSecret(
        string folderId,
        [AliasAs("SecretMetadata")] CreateSecretFileRequest metadata,
        [AliasAs("File")] StreamPart file);

    [Post($"{V3}/Secrets-Safe/Folders/")]
    public Task<FolderResponse> CreateFolder([Body] FolderRequest request);

    [Get($"{V3}/Secrets-Safe/Folders/{{id}}")]
    public Task<FolderResponse> GetFolder(string id);

    [Put($"{V3}/Secrets-Safe/Folders/{{id}}")]
    public Task<FolderResponse> UpdateFolder(string id, [Body] FolderRequest request);

    [Delete($"{V3}/Secrets-Safe/Folders/{{id}}")]
    public Task<HttpResponseMessage> DeleteFolder(string id);

    [Post($"{V3}/Auth/Signout")]
    public Task<ApiResponse<HttpResponseMessage>> Signout();
}
