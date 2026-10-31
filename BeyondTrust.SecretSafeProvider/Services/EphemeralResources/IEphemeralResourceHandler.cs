using BeyondTrust.SecretSafeProvider.Proto;

namespace BeyondTrust.SecretSafeProvider.Services.EphemeralResources;

public interface IEphemeralResourceHandler
{
    string TypeName { get; }
    Schema GetSchema();
    Task<OpenEphemeralResource.Types.Response> OpenAsync(OpenEphemeralResource.Types.Request request);
}
