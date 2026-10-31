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

    public static async Task DeleteSecretIfExists(this IBeyondTrustSecretSafe secretSafe, string secretId)
    {
        using var response = await secretSafe.DeleteSecret(secretId);

        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
            response.EnsureSuccessStatusCode();
    }

    public static async Task<long> ResolveOwnerId(this IBeyondTrustApiFactory apiFactory, SignAppinResponse? session, long? plannedOwnerId)
        => plannedOwnerId ?? session?.UserId ?? await apiFactory.GetOAuthUserId();
}
