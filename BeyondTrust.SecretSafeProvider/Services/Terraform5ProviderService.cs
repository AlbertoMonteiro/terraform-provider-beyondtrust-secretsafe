using Google.Protobuf;
using Grpc.Core;
using System.Text.Json;

namespace BeyondTrust.SecretSafeProvider.Services;

public class Terraform5ProviderService(ILogger<Terraform5ProviderService> logger, IServiceCaller serviceCaller) : Provider.ProviderBase
{
    private const string DataSourceName = "beyondtrust_hello";
    private readonly ILogger<Terraform5ProviderService> _logger = logger;
    private readonly IServiceCaller _serviceCaller = serviceCaller;

    public override Task<GetProviderSchema.Types.Response> GetSchema(GetProviderSchema.Types.Request request, ServerCallContext context)
    {
        var response = new GetProviderSchema.Types.Response
        {
            Provider = new Schema { Block = new Schema.Types.Block() },
        };

        response.DataSourceSchemas[DataSourceName] = MyClass.GetSchema();

        return Task.FromResult(response);
    }

    public override Task<PrepareProviderConfig.Types.Response> PrepareProviderConfig(PrepareProviderConfig.Types.Request request, ServerCallContext context)
        => Task.FromResult(new PrepareProviderConfig.Types.Response { PreparedConfig = request.Config });

    public override Task<Configure.Types.Response> Configure(Configure.Types.Request request, ServerCallContext context)
        => Task.FromResult(new Configure.Types.Response());

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
        _logger.LogInformation("ReadDataSource: {TypeName}", request.TypeName);

        var weather = await _serviceCaller.GetWeatherForecastsAsync();

        MyClass response = new() { Content = weather.First().ToString() };

        return new ReadDataSource.Types.Response
        {
            State = new DynamicValue
            {
                Msgpack = ByteString.CopyFrom(MessagePack.MessagePackSerializer.Serialize(response)),
                Json = ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes(response, Json.Default.MyClass))
            }
        };
    }

    public override Task<Stop.Types.Response> Stop(Stop.Types.Request request, ServerCallContext context)
        => Task.FromResult(new Stop.Types.Response());
}
