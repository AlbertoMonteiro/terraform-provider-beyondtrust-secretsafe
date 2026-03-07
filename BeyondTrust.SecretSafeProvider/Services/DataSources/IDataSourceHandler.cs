namespace BeyondTrust.SecretSafeProvider.Services;

public interface IDataSourceHandler
{
    string TypeName { get; }
    Schema GetSchema();
    Task<ReadDataSource.Types.Response> ReadAsync(ReadDataSource.Types.Request request);
}
