namespace BeyondTrust.SecretSafeProvider.Models;

/// <summary>
/// Resposta da API de autenticação SignAppin.
/// Retorna informações do usuário autenticado, incluindo UserId que é usado como OwnerId em operações subsequentes.
/// </summary>
public record SignAppinResponse(
    int UserId,
    string? SID,
    string EmailAddress,
    string UserName,
    string Name
);
