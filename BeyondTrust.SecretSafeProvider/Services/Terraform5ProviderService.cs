using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services.DataSources;
using BeyondTrust.SecretSafeProvider.Services.Resources;
using Grpc.Core;

namespace BeyondTrust.SecretSafeProvider.Services;

public class Terraform5ProviderService(
    IEnumerable<IDataSourceHandler> dataSourceHandlers,
    IEnumerable<IResourceHandler> resourceHandlers,
    ProviderConfiguration configuration) : Provider.ProviderBase
{
    private readonly IReadOnlyDictionary<string, IDataSourceHandler> _dataSourceHandlers =
        dataSourceHandlers.ToDictionary(h => h.TypeName);
    private readonly IReadOnlyDictionary<string, IResourceHandler> _resourceHandlers =
        resourceHandlers.ToDictionary(h => h.TypeName);
    private readonly ProviderConfiguration _configuration = configuration;

    public override Task<GetProviderSchema.Types.Response> GetSchema(GetProviderSchema.Types.Request request, ServerCallContext context)
    {
        var response = new GetProviderSchema.Types.Response();

        foreach (var handler in _dataSourceHandlers.Values)
            response.DataSourceSchemas[handler.TypeName] = handler.GetSchema();

        foreach (var handler in _resourceHandlers.Values)
            response.ResourceSchemas[handler.TypeName] = handler.GetSchema();

        response.Provider = ProviderConfiguration.GetSchema();

        return Task.FromResult(response);
    }

    public override Task<PrepareProviderConfig.Types.Response> PrepareProviderConfig(PrepareProviderConfig.Types.Request request, ServerCallContext context)
        => Task.FromResult(new PrepareProviderConfig.Types.Response { PreparedConfig = request.Config });

    public override Task<Configure.Types.Response> Configure(Configure.Types.Request request, ServerCallContext context)
    {
        var cfg = SmartSerializer.Deserialize<ProviderConfiguration>(request.Config);
        _configuration.ReplaceValues(cfg);
        return Task.FromResult(new Configure.Types.Response());
    }

    public override Task<ValidateDataSourceConfig.Types.Response> ValidateDataSourceConfig(ValidateDataSourceConfig.Types.Request request, ServerCallContext context)
        => Task.FromResult(new ValidateDataSourceConfig.Types.Response());

    public override Task<ValidateResourceTypeConfig.Types.Response> ValidateResourceTypeConfig(ValidateResourceTypeConfig.Types.Request request, ServerCallContext context)
        => Task.FromResult(new ValidateResourceTypeConfig.Types.Response());

    public override Task<UpgradeResourceState.Types.Response> UpgradeResourceState(UpgradeResourceState.Types.Request request, ServerCallContext context)
        => Task.FromResult(new UpgradeResourceState.Types.Response());

    public override async Task<ReadResource.Types.Response> ReadResource(ReadResource.Types.Request request, ServerCallContext context)
    {
        if (!_resourceHandlers.TryGetValue(request.TypeName, out var handler))
            return new ReadResource.Types.Response
            {
                Diagnostics =
                {
                    new Diagnostic
                    {
                        Severity = Diagnostic.Types.Severity.Error,
                        Summary = $"Unsupported resource \"{request.TypeName}\"",
                        Detail = $"""
                                The provider does not implement the resource "{request.TypeName}".
                                Available resources: {string.Join(", ", _resourceHandlers.Keys)}.
                                """
                    }
                }
            };

        try
        {
            return await handler.ReadAsync(request);
        }
        catch (Exception ex)
        {
            return new ReadResource.Types.Response
            {
                Diagnostics =
                {
                    new Diagnostic
                    {
                        Severity = Diagnostic.Types.Severity.Error,
                        Summary = $"Error reading resource \"{request.TypeName}\"",
                        Detail = ex.Message,
                    }
                }
            };
        }
    }

    public override Task<PlanResourceChange.Types.Response> PlanResourceChange(PlanResourceChange.Types.Request request, ServerCallContext context)
        => Task.FromResult(new PlanResourceChange.Types.Response { PlannedState = request.ProposedNewState });

    public override async Task<ApplyResourceChange.Types.Response> ApplyResourceChange(ApplyResourceChange.Types.Request request, ServerCallContext context)
    {
        if (!_resourceHandlers.TryGetValue(request.TypeName, out var handler))
            return new ApplyResourceChange.Types.Response
            {
                Diagnostics =
                {
                    new Diagnostic
                    {
                        Severity = Diagnostic.Types.Severity.Error,
                        Summary = $"Unsupported resource \"{request.TypeName}\"",
                        Detail = $"""
                                The provider does not implement the resource "{request.TypeName}".
                                Available resources: {string.Join(", ", _resourceHandlers.Keys)}.
                                """
                    }
                }
            };

        try
        {
            return await handler.ApplyAsync(request);
        }
        catch (Exception ex)
        {
            return new ApplyResourceChange.Types.Response
            {
                Diagnostics =
                {
                    new Diagnostic
                    {
                        Severity = Diagnostic.Types.Severity.Error,
                        Summary = $"Error applying resource \"{request.TypeName}\"",
                        Detail = ex.Message,
                    }
                }
            };
        }
    }

    public override Task<ImportResourceState.Types.Response> ImportResourceState(ImportResourceState.Types.Request request, ServerCallContext context)
        => Task.FromResult(new ImportResourceState.Types.Response());

    public override async Task<ReadDataSource.Types.Response> ReadDataSource(ReadDataSource.Types.Request request, ServerCallContext context)
    {
        if (!_dataSourceHandlers.TryGetValue(request.TypeName, out var handler))
            return new ReadDataSource.Types.Response
            {
                Diagnostics =
                {
                    new Diagnostic
                    {
                        Severity = Diagnostic.Types.Severity.Error,
                        Summary  = $"Unsupported data source \"{request.TypeName}\"",
                        Detail   = $"""
                                   The provider does not implement the data source "{request.TypeName}".
                                   Available data sources: {string.Join(", ", _dataSourceHandlers.Keys)}.
                                   """
                    }
                }
            };

        try
        {
            return await handler.ReadAsync(request);
        }
        catch (Exception ex)
        {
            return new ReadDataSource.Types.Response
            {
                Diagnostics =
                {
                    new Diagnostic
                    {
                        Severity = Diagnostic.Types.Severity.Error,
                        Summary  = $"Error reading data source \"{request.TypeName}\"",
                        Detail   = ex.Message,
                    }
                }
            };
        }
    }

    public override Task<Stop.Types.Response> Stop(Stop.Types.Request request, ServerCallContext context)
        => Task.FromResult(new Stop.Types.Response());
}
