using System.Text.Json;

namespace BeyondTrust.SecretSafeProvider.Services;

public interface IServiceCaller
{
    public Task<WeatherForecast[]> GetWeatherForecastsAsync();
}

public class ServiceCaller(HttpClient httpClient) : IServiceCaller
{
    private static readonly JsonSerializerOptions options = Json.Default.Options;
    private readonly HttpClient httpClient = httpClient;

    public async Task<WeatherForecast[]> GetWeatherForecastsAsync()
    {
        var items = await httpClient.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast", options: options);
        return items!;
    }
}