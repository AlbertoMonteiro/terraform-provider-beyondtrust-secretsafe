using Grpc.Core;

namespace BeyondTrust.SecretSafeProvider.Services;

public class Terraform5ProviderService(ILogger<Terraform5ProviderService> logger) : Provider.ProviderBase
{
    private readonly ILogger<Terraform5ProviderService> _logger = logger;
}
