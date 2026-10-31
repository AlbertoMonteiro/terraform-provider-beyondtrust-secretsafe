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
        };
    }

    public static async Task<FileDownloadData> ReadFile(this IBeyondTrustSecretSafe secretSafe, ProviderConfiguration configuration, string secretId)
    {
        await secretSafe.OpenSession(configuration);
        var response = await secretSafe.DownloadSecret(Guid.Parse(secretId));
        await secretSafe.CloseSession(configuration);

        var contentBytes = await response.Content.ReadAsByteArrayAsync();
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? string.Empty;

        return new FileDownloadData
        {
            SecretId = secretId,
            FileName = fileName,
            FileContentBase64 = Convert.ToBase64String(contentBytes),
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
