using BeyondTrust.SecretSafeProvider.Proto;

namespace BeyondTrust.SecretSafeProvider.Services.Resources;

public interface IResourceHandler
{
    string TypeName { get; }
    Schema GetSchema();
    Task<ReadResource.Types.Response> ReadAsync(ReadResource.Types.Request request);
    Task<ApplyResourceChange.Types.Response> ApplyAsync(ApplyResourceChange.Types.Request request);
}
