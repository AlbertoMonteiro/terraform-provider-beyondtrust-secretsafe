using BeyondTrust.SecretSafeProvider.Models;
using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;
using BeyondTrust.SecretSafeProvider.Services;
using BeyondTrust.SecretSafeProvider.Services.DataSources;
using BeyondTrust.SecretSafeProvider.Services.EphemeralResources;
using BeyondTrust.SecretSafeProvider.Services.Resources;
using Imposter.Abstractions;

namespace BeyondTrust.SecretSafeProvider.Tests;

public class Terraform5ProviderServiceTests
{
    private readonly Terraform5ProviderService _sut;
    private readonly IDataSourceHandler _credentialDataSourceHandler;
    private readonly IDataSourceHandlerImposter _credentialImposter;
    private readonly IEphemeralResourceHandlerImposter _ephemeralCredentialImposter;
    private readonly ProviderConfiguration _configuration;

    public Terraform5ProviderServiceTests()
    {
        _credentialImposter = IDataSourceHandler.Imposter();

        _credentialImposter.TypeName.Getter().Returns("secretsafe_credential_data");

        _credentialDataSourceHandler = _credentialImposter.Instance();

        _ephemeralCredentialImposter = IEphemeralResourceHandler.Imposter();

        _ephemeralCredentialImposter.TypeName.Getter().Returns("secretsafe_credential_data");
        _ephemeralCredentialImposter.GetSchema().Returns(new Schema { Version = 1, Block = new Schema.Types.Block() });

        _configuration = new ProviderConfiguration
        {
            Key = "",
            RunAs = "",
            BaseUrl = ""
        };

        _sut = new Terraform5ProviderService([_credentialImposter.Instance()], [], [_ephemeralCredentialImposter.Instance()], _configuration);
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
        await Assert.That(response.Provider.Block.Attributes).Count().IsEqualTo(6);
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
    public async Task UpgradeResourceState_ReturnsRawStateUnchanged()
    {
        // Arrange
        var rawState = """{ "id": "secret-id" }"""u8.ToArray();
        var request = new UpgradeResourceState.Types.Request
        {
            RawState = new RawState { Json = Google.Protobuf.ByteString.CopyFrom(rawState) }
        };

        // Act
        var response = await _sut.UpgradeResourceState(request, null!);

        // Assert
        await Assert.That(response.UpgradedState.Json.ToByteArray()).IsEquivalentTo(rawState);
    }

    [Test]
    public async Task Configure_WithOAuthCredentials_UpdatesSharedConfiguration()
    {
        // Arrange
        var newConfig = new ProviderConfiguration
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            BaseUrl = "https://beyondtrust.example.com"
        };

        var request = new Configure.Types.Request
        {
            Config = SmartSerializer.Serialize(newConfig)
        };

        // Act
        var response = await _sut.Configure(request, null!);

        // Assert
        await Assert.That(response.Diagnostics).IsEmpty();
        await Assert.That(_configuration.ClientId).IsEqualTo(newConfig.ClientId);
        await Assert.That(_configuration.ClientSecret).IsEqualTo(newConfig.ClientSecret);
        await Assert.That(_configuration.UsesOAuth).IsTrue();
    }

    [Test]
    public async Task Configure_WithInvalidConfiguration_ReturnsDiagnosticAndKeepsPreviousValues()
    {
        // Arrange
        var newConfig = new ProviderConfiguration
        {
            Key = "my-api-key",
            RunAs = "svc-account",
            ClientId = "client-id",
            ClientSecret = "client-secret",
            BaseUrl = "https://beyondtrust.example.com"
        };

        var request = new Configure.Types.Request
        {
            Config = SmartSerializer.Serialize(newConfig)
        };

        // Act
        var response = await _sut.Configure(request, null!);

        // Assert
        await Assert.That(response.Diagnostics).Count().IsEqualTo(2);
        await Assert.That(response.Diagnostics[0].Severity).IsEqualTo(Diagnostic.Types.Severity.Error);
        await Assert.That(_configuration.ClientId).IsNull();
        await Assert.That(_configuration.BaseUrl).IsEqualTo("");
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

    [Test]
    public async Task GetSchema_WithEphemeralHandler_ReturnsEphemeralResourceSchema()
    {
        // Arrange
        _credentialImposter.GetSchema().Returns(new Schema { Block = new Schema.Types.Block() });

        // Act
        var response = await _sut.GetSchema(new GetProviderSchema.Types.Request(), null!);

        // Assert
        await Assert.That(response.EphemeralResourceSchemas.ContainsKey("secretsafe_credential_data")).IsTrue();
    }

    [Test]
    public async Task OpenEphemeralResource_WithRegisteredHandler_ReturnsResponseWithoutRenewalData()
    {
        // Arrange
        var request = new OpenEphemeralResource.Types.Request { TypeName = "secretsafe_credential_data" };
        var result = new DynamicValue { Msgpack = Google.Protobuf.ByteString.CopyFromUtf8("result-bytes") };

        _ephemeralCredentialImposter.OpenAsync(request).ReturnsAsync(new OpenEphemeralResource.Types.Response { Result = result });

        // Act
        var response = await _sut.OpenEphemeralResource(request, null!);

        // Assert
        await Assert.That(response.Diagnostics).IsEmpty();
        await Assert.That(response.Result).IsEqualTo(result);
        await Assert.That(response.RenewAt).IsNull();
        await Assert.That(response.HasPrivate).IsFalse();
    }

    [Test]
    public async Task OpenEphemeralResource_WhenTypeNameIsNotRegistered_ReturnsDiagnosticWithUnsupportedError()
    {
        // Arrange
        var request = new OpenEphemeralResource.Types.Request { TypeName = "unknown_ephemeral" };

        // Act
        var response = await _sut.OpenEphemeralResource(request, null!);

        // Assert
        await Assert.That(response.Diagnostics).Count().IsEqualTo(1);
        await Assert.That(response.Diagnostics[0].Severity).IsEqualTo(Diagnostic.Types.Severity.Error);
        await Assert.That(response.Diagnostics[0].Summary).IsEqualTo("Unsupported ephemeral resource \"unknown_ephemeral\"");
    }

    [Test]
    public async Task OpenEphemeralResource_WhenHandlerThrows_ReturnsDiagnosticWithError()
    {
        // Arrange
        const string exceptionMessage = "Secret not found";
        var request = new OpenEphemeralResource.Types.Request { TypeName = "secretsafe_credential_data" };

        _ephemeralCredentialImposter.OpenAsync(request).Throws(new Exception(exceptionMessage));

        // Act
        var response = await _sut.OpenEphemeralResource(request, null!);

        // Assert
        await Assert.That(response.Diagnostics).Count().IsEqualTo(1);
        await Assert.That(response.Diagnostics[0].Severity).IsEqualTo(Diagnostic.Types.Severity.Error);
        await Assert.That(response.Diagnostics[0].Summary).IsEqualTo("Error opening ephemeral resource \"secretsafe_credential_data\"");
        await Assert.That(response.Diagnostics[0].Detail).IsEqualTo(exceptionMessage);
    }

    [Test]
    public async Task ValidateEphemeralResourceConfig_WithUnreadableConfig_ReturnsEmptyResponse()
    {
        // Arrange
        // Terraform sends unknown values here when secret_id depends on another object
        var request = new ValidateEphemeralResourceConfig.Types.Request
        {
            TypeName = "secretsafe_credential_data",
            Config = new DynamicValue { Msgpack = Google.Protobuf.ByteString.CopyFromUtf8("not-msgpack") }
        };

        // Act
        var response = await _sut.ValidateEphemeralResourceConfig(request, null!);

        // Assert
        await Assert.That(response.Diagnostics).IsEmpty();
    }

    [Test]
    public async Task RenewEphemeralResource_ReturnsEmptyResponse()
    {
        // Act
        var response = await _sut.RenewEphemeralResource(new RenewEphemeralResource.Types.Request(), null!);

        // Assert
        await Assert.That(response.Diagnostics).IsEmpty();
        await Assert.That(response.RenewAt).IsNull();
    }

    [Test]
    public async Task CloseEphemeralResource_ReturnsEmptyResponse()
    {
        // Act
        var response = await _sut.CloseEphemeralResource(new CloseEphemeralResource.Types.Request(), null!);

        // Assert
        await Assert.That(response.Diagnostics).IsEmpty();
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
    public async Task ReadResource_WithoutResourceHandlers_ReturnsDiagnosticError()
    {
        // Arrange
        var state = new DynamicValue { Msgpack = Google.Protobuf.ByteString.CopyFromUtf8("state-bytes") };
        var request = new ReadResource.Types.Request {
            CurrentState = state,
            TypeName = "unknown_resource"
        };

        // Act
        var result = await _sut.ReadResource(request, null!);

        // Assert
        await Assert.That(result.Diagnostics).Count().IsGreaterThan(0);
        await Assert.That(result.Diagnostics[0].Severity).IsEqualTo(Diagnostic.Types.Severity.Error);
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
    public async Task ApplyResourceChange_WithoutResourceHandlers_ReturnsDiagnosticError()
    {
        // Arrange
        var planned = new DynamicValue { Msgpack = Google.Protobuf.ByteString.CopyFromUtf8("planned-bytes") };
        var request = new ApplyResourceChange.Types.Request { PlannedState = planned, TypeName = "unknown_resource" };

        // Act
        var result = await _sut.ApplyResourceChange(request, null!);

        // Assert
        await Assert.That(result.Diagnostics).Count().IsGreaterThan(0);
        await Assert.That(result.Diagnostics[0].Severity).IsEqualTo(Diagnostic.Types.Severity.Error);
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
