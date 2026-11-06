namespace BeyondTrust.SecretSafeProvider.Models;

public record FolderCredentialEntry(string SecretId, string Username, string Password)
{
    public const string SECRET_ID = "secret_id";
    public const string USERNAME  = "username";
    public const string PASSWORD  = "password";
}
