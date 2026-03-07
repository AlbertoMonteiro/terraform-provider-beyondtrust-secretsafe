namespace BeyondTrust.SecretSafeProvider.Models;

public record KeyAndRunAs(string Key, string RunAs)
{
    public override string ToString()
        => $"key={Key}; runas={RunAs};";
}
