using BeyondTrust.SecretSafeProvider.Models;

namespace BeyondTrust.SecretSafeProvider.Services;

/// <summary>
/// API key authentication needs a server side session. OAuth has none: every request carries a
/// bearer token added by <see cref="OAuthBearerHandler"/>.
/// </summary>
public static class BeyondTrustSessionExtensions
{
    public static async Task<SignAppinResponse?> OpenSession(this IBeyondTrustSecretSafe secretSafe, ProviderConfiguration configuration)
        => configuration.UsesOAuth
            ? null
            : await secretSafe.SignAppin(new KeyAndRunAs(configuration.Key, configuration.RunAs, configuration.Pwd));

    public static async Task CloseSession(this IBeyondTrustSecretSafe secretSafe, ProviderConfiguration configuration)
    {
        if (!configuration.UsesOAuth)
            await secretSafe.Signout();
    }

    public static async Task<CredentialData> ReadCredential(this IBeyondTrustSecretSafe secretSafe, ProviderConfiguration configuration, string secretId)
    {
        await secretSafe.OpenSession(configuration);
        var secretResponse = await secretSafe.GetSecret(Guid.Parse(secretId));
        await secretSafe.CloseSession(configuration);

        return new CredentialData
        {
            SecretId = secretId,
            Username = secretResponse.Username ?? string.Empty,
            Password = secretResponse.Password ?? string.Empty,
        }.CopyFrom(secretResponse);
    }

    public static async Task<FileDownloadData> ReadFile(this IBeyondTrustSecretSafe secretSafe, ProviderConfiguration configuration, string secretId)
    {
        await secretSafe.OpenSession(configuration);
        // The download only carries the content, so the metadata needs its own request
        var metadata = await secretSafe.GetSecret(Guid.Parse(secretId));
        var response = await secretSafe.DownloadSecret(Guid.Parse(secretId));
        await secretSafe.CloseSession(configuration);

        var contentBytes = await response.Content.ReadAsByteArrayAsync();
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? string.Empty;

        return new FileDownloadData
        {
            SecretId = secretId,
            FileName = fileName,
            FileContentBase64 = Convert.ToBase64String(contentBytes),
            FileHash = metadata.FileHash,
        }.CopyFrom(metadata);
    }

    public static async Task<FolderCredentialsData> ReadFolderCredentials(this IBeyondTrustSecretSafe secretSafe, ProviderConfiguration configuration, string? folderId, string? folderName)
    {
        var (resolvedFolderId, secrets) = await secretSafe.ReadFolderCredentialSecrets(configuration, folderId, folderName);

        return new FolderCredentialsData
        {
            FolderId = resolvedFolderId,
            FolderName = folderName,
            Credentials = secrets.ToDictionary(
                pair => pair.Key,
                pair => new FolderCredentialEntry(
                    pair.Value.Id ?? string.Empty, pair.Value.Username ?? string.Empty, pair.Value.Password ?? string.Empty,
                    pair.Value.CreatedOn, pair.Value.CreatedBy, pair.Value.ModifiedOn, pair.Value.ModifiedBy),
                StringComparer.Ordinal),
        };
    }

    // Same keys as ReadFolderCredentials, but the username and password are never copied
    public static async Task<FolderCredentialsMetadataData> ReadFolderCredentialsMetadata(this IBeyondTrustSecretSafe secretSafe, ProviderConfiguration configuration, string? folderId, string? folderName)
    {
        var (resolvedFolderId, secrets) = await secretSafe.ReadFolderCredentialSecrets(configuration, folderId, folderName);

        return new FolderCredentialsMetadataData
        {
            FolderId = resolvedFolderId,
            FolderName = folderName,
            Credentials = secrets.ToDictionary(
                pair => pair.Key,
                pair => new FolderCredentialMetadataEntry(
                    pair.Value.Id ?? string.Empty, pair.Value.CreatedOn, pair.Value.CreatedBy, pair.Value.ModifiedOn, pair.Value.ModifiedBy),
                StringComparer.Ordinal),
        };
    }

    // The credential secrets of a folder keyed by title, found by folder_id or folder_name
    private static async Task<(string FolderId, Dictionary<string, SecretValue> Secrets)> ReadFolderCredentialSecrets(this IBeyondTrustSecretSafe secretSafe, ProviderConfiguration configuration, string? folderId, string? folderName)
    {
        if (string.IsNullOrEmpty(folderId) == string.IsNullOrEmpty(folderName))
            throw new ArgumentException("Set exactly one of \"folder_id\" or \"folder_name\".");

        List<SecretValue> secrets;

        await secretSafe.OpenSession(configuration);
        try
        {
            if (string.IsNullOrEmpty(folderId))
                folderId = FindFolderId(await secretSafe.FindFolders(folderName!), folderName!);

            secrets = await secretSafe.GetFolderSecrets(folderId);
        }
        finally
        {
            await secretSafe.CloseSession(configuration);
        }

        var credentials = new Dictionary<string, SecretValue>(StringComparer.Ordinal);

        foreach (var secret in secrets.Where(s => string.Equals(s.SecretType, "Credential", StringComparison.OrdinalIgnoreCase)))
        {
            var title = secret.Title ?? string.Empty;

            if (!credentials.TryAdd(title, secret))
                throw new InvalidOperationException($"Folder {folderId} has more than one credential titled \"{title}\".");
        }

        return (folderId, credentials);
    }

    // The search matches part of the name, so only an exact match, ignoring case like the API, is accepted
    private static string FindFolderId(List<FolderResponse> folders, string folderName)
    {
        var matches = folders.Where(f => string.Equals(f.Name, folderName, StringComparison.OrdinalIgnoreCase)).ToList();

        return matches.Count switch
        {
            0 => throw new InvalidOperationException($"Folder \"{folderName}\" was not found."),
            1 => matches[0].Id,
            _ => throw new InvalidOperationException($"Folder name \"{folderName}\" matches {matches.Count} folders: {string.Join(", ", matches.Select(f => f.Id))}. Use folder_id to choose one."),
        };
    }

    public static async Task DeleteSecretIfExists(this IBeyondTrustSecretSafe secretSafe, string secretId)
    {
        using var response = await secretSafe.DeleteSecret(secretId);

        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
            response.EnsureSuccessStatusCode();
    }

    public static async Task<long> ResolveOwnerId(this IBeyondTrustApiFactory apiFactory, SignAppinResponse? session, long? plannedOwnerId)
        => plannedOwnerId ?? session?.UserId ?? await apiFactory.GetOAuthUserId();
}
