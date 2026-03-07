using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Serialization;
using Grpc.Core;
using MessagePack;
using System.Text.Json;

namespace BeyondTrust.SecretSafeProvider.Services;

public class Terraform5ProviderService(
    IEnumerable<IDataSourceHandler> dataSourceHandlers,
    ProviderConfiguration configuration) : Provider.ProviderBase
{
    private readonly IReadOnlyDictionary<string, IDataSourceHandler> _handlers =
        dataSourceHandlers.ToDictionary(h => h.TypeName);
    private readonly ProviderConfiguration _configuration = configuration;

    public override Task<GetProviderSchema.Types.Response> GetSchema(GetProviderSchema.Types.Request request, ServerCallContext context)
    {
        var response = new GetProviderSchema.Types.Response();

        foreach (var handler in _handlers.Values)
            response.DataSourceSchemas[handler.TypeName] = handler.GetSchema();

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

    public override Task<ReadResource.Types.Response> ReadResource(ReadResource.Types.Request request, ServerCallContext context)
        => Task.FromResult(new ReadResource.Types.Response { NewState = request.CurrentState });

    public override Task<PlanResourceChange.Types.Response> PlanResourceChange(PlanResourceChange.Types.Request request, ServerCallContext context)
        => Task.FromResult(new PlanResourceChange.Types.Response { PlannedState = request.ProposedNewState });

    public override Task<ApplyResourceChange.Types.Response> ApplyResourceChange(ApplyResourceChange.Types.Request request, ServerCallContext context)
        => Task.FromResult(new ApplyResourceChange.Types.Response { NewState = request.PlannedState });

    public override Task<ImportResourceState.Types.Response> ImportResourceState(ImportResourceState.Types.Request request, ServerCallContext context)
        => Task.FromResult(new ImportResourceState.Types.Response());

    public override async Task<ReadDataSource.Types.Response> ReadDataSource(ReadDataSource.Types.Request request, ServerCallContext context)
    {
        if (!_handlers.TryGetValue(request.TypeName, out var handler))
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
                                   Available data sources: {string.Join(", ", _handlers.Keys)}.
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
