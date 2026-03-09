using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services;
using BeyondTrust.SecretSafeProvider.Services.DataSources;
using BeyondTrust.SecretSafeProvider.Services.Resources;
using Imposter.Abstractions;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class Terraform5ProviderServiceTests
{
    private readonly Terraform5ProviderService _sut;
    private readonly IDataSourceHandler _credentialDataSourceHandler;
    private readonly IDataSourceHandlerImposter _credentialImposter;
    private readonly ProviderConfiguration _configuration;

    public Terraform5ProviderServiceTests()
    {
        _credentialImposter = IDataSourceHandler.Imposter();

        _credentialImposter.TypeName.Getter().Returns("secretsafe_credential_data");

        _credentialDataSourceHandler = _credentialImposter.Instance();

        _configuration = new ProviderConfiguration
        {
            Key = "",
            RunAs = "",
            BaseUrl = ""
        };

        _sut = new Terraform5ProviderService([_credentialImposter.Instance()], [], _configuration);
    }

    [Test]
    public async Task GetSchema_WithRegisteredHandler_ReturnsSchemaForHandlerAndProvider()
    {
        // Arrange
        var request = new GetProviderSchema.Types.Request();

        _credentialImposter.GetSchema().Returns(new Schema()
        {
            Version = 1,
            Block = new Schema.Types.Block()
        });

        // Act
        var response = await _sut.GetSchema(request, null!);

        // Assert
        await Assert.That(response.DataSourceSchemas.ContainsKey(_credentialDataSourceHandler.TypeName)).IsTrue();
        await Assert.That(response.DataSourceSchemas[_credentialDataSourceHandler.TypeName]).IsNotNull();
        await Assert.That(response.Provider).IsNotNull();
        await Assert.That(response.Provider.Block.Attributes).Count().IsEqualTo(3);
    }

    [Test]
    public async Task Configure_WithNewValues_UpdatesSharedConfiguration()
    {
        // Arrange
        var newConfig = new ProviderConfiguration
        {
            Key = "my-api-key",
            RunAs = "svc-account",
            BaseUrl = "https://beyondtrust.example.com"
        };

        var request = new Configure.Types.Request
        {
            Config = SmartSerializer.Serialize(newConfig)
        };

        // Act
        await _sut.Configure(request, null!);

        // Assert
        await Assert.That(_configuration.Key).IsEqualTo(newConfig.Key);
        await Assert.That(_configuration.RunAs).IsEqualTo(newConfig.RunAs);
        await Assert.That(_configuration.BaseUrl).IsEqualTo(newConfig.BaseUrl);
    }

    [Test]
    public async Task ReadDataSource_WithRegisteredHandler_ReturnsHandlerResponse()
    {
        // Arrange
        var request = new ReadDataSource.Types.Request { TypeName = "secretsafe_credential_data" };
        var expectedResponse = new ReadDataSource.Types.Response();

        _credentialImposter.ReadAsync(request).ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.ReadDataSource(request, null!);

        // Assert
        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(result).IsEqualTo(expectedResponse);
    }

    [Test]
    public async Task ReadDataSource_WhenTypeNameIsNotRegistered_ReturnsDiagnosticWithUnsupportedError()
    {
        // Arrange
        var request = new ReadDataSource.Types.Request { TypeName = "unknown_data_source" };

        // Act
        var result = await _sut.ReadDataSource(request, null!);

        // Assert
        await Assert.That(result.Diagnostics).Count().IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Severity).IsEqualTo(Diagnostic.Types.Severity.Error);
        await Assert.That(result.Diagnostics[0].Summary).IsEqualTo("Unsupported data source \"unknown_data_source\"");
    }

    [Test]
    public async Task ReadDataSource_WhenHandlerThrows_ReturnsDiagnosticWithError()
    {
        // Arrange
        const string exceptionMessage = "Unexpected handler failure";
        var request = new ReadDataSource.Types.Request { TypeName = "secretsafe_credential_data" };

        _credentialImposter.ReadAsync(request).Throws(new Exception(exceptionMessage));

        // Act
        var result = await _sut.ReadDataSource(request, null!);

        // Assert
        await Assert.That(result.Diagnostics).Count().IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Severity).IsEqualTo(Diagnostic.Types.Severity.Error);
        await Assert.That(result.Diagnostics[0].Summary).IsEqualTo("Error reading data source \"secretsafe_credential_data\"");
        await Assert.That(result.Diagnostics[0].Detail).IsEqualTo(exceptionMessage);
    }

    // Pass-through methods

    [Test]
    public async Task PrepareProviderConfig_ReturnsRequestConfigAsPreparedConfig()
    {
        // Arrange
        var config = new DynamicValue { Msgpack = Google.Protobuf.ByteString.CopyFromUtf8("config-bytes") };
        var request = new PrepareProviderConfig.Types.Request { Config = config };

        // Act
        var result = await _sut.PrepareProviderConfig(request, null!);

        // Assert
        await Assert.That(result.PreparedConfig).IsEqualTo(config);
    }

    [Test]
    public async Task ReadResource_ReturnsCurrentStateAsNewState()
    {
        // Arrange
        var state = new DynamicValue { Msgpack = Google.Protobuf.ByteString.CopyFromUtf8("state-bytes") };
        var request = new ReadResource.Types.Request { CurrentState = state };

        // Act
        var result = await _sut.ReadResource(request, null!);

        // Assert
        await Assert.That(result.NewState).IsEqualTo(state);
    }

    [Test]
    public async Task PlanResourceChange_ReturnsProposedNewStateAsPlannedState()
    {
        // Arrange
        var proposed = new DynamicValue { Msgpack = Google.Protobuf.ByteString.CopyFromUtf8("proposed-bytes") };
        var request = new PlanResourceChange.Types.Request { ProposedNewState = proposed };

        // Act
        var result = await _sut.PlanResourceChange(request, null!);

        // Assert
        await Assert.That(result.PlannedState).IsEqualTo(proposed);
    }

    [Test]
    public async Task ApplyResourceChange_ReturnsPlannedStateAsNewState()
    {
        // Arrange
        var planned = new DynamicValue { Msgpack = Google.Protobuf.ByteString.CopyFromUtf8("planned-bytes") };
        var request = new ApplyResourceChange.Types.Request { PlannedState = planned };

        // Act
        var result = await _sut.ApplyResourceChange(request, null!);

        // Assert
        await Assert.That(result.NewState).IsEqualTo(planned);
    }

    // Empty-response methods

    [Test]
    public async Task ValidateDataSourceConfig_ReturnsEmptyResponse()
    {
        // Act
        var result = await _sut.ValidateDataSourceConfig(new ValidateDataSourceConfig.Types.Request(), null!);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Diagnostics).IsEmpty();
    }

    [Test]
    public async Task ValidateResourceTypeConfig_ReturnsEmptyResponse()
    {
        // Act
        var result = await _sut.ValidateResourceTypeConfig(new ValidateResourceTypeConfig.Types.Request(), null!);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Diagnostics).IsEmpty();
    }

    [Test]
    public async Task UpgradeResourceState_ReturnsEmptyResponse()
    {
        // Act
        var result = await _sut.UpgradeResourceState(new UpgradeResourceState.Types.Request(), null!);

        // Assert
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task ImportResourceState_ReturnsEmptyResponse()
    {
        // Act
        var result = await _sut.ImportResourceState(new ImportResourceState.Types.Request(), null!);

        // Assert
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task Stop_ReturnsEmptyResponse()
    {
        // Act
        var result = await _sut.Stop(new Stop.Types.Request(), null!);

        // Assert
        await Assert.That(result).IsNotNull();
    }
}
