namespace BeyondTrust.SecretSafeProvider.Services;

public interface IBeyondTrustApiFactory
{
    IBeyondTrustSecretSafe CreateApi();

    Task<long> GetOAuthUserId();
}
