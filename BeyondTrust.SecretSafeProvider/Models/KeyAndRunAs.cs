namespace BeyondTrust.SecretSafeProvider.Models;

public record KeyAndRunAs(string Key, string RunAs, string? Pwd = null)
{
    public override string ToString()
        => Pwd is null
            ? $"key={Key}; runas={RunAs};"
            : $"key={Key}; runas={RunAs}; pwd={Pwd};";
}
