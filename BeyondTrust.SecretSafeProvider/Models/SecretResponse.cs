namespace BeyondTrust.SecretSafeProvider.Models;

public record SecretResponse(
    string Id,
    string? Title,
    string? Description,
    long OwnerId);
