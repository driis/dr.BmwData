using System.Net.Http.Headers;
using System.Net.Http.Json;
using dr.BmwData.Models;
using Microsoft.Extensions.Options;

namespace dr.BmwData;

public class TelemetryService : ITelemetryService
{
    private readonly HttpClient _httpClient;
    private readonly BmwOptions _options;
    private readonly IAuthenticationService _authService;

    public TelemetryService(HttpClient httpClient, IOptions<BmwOptions> options, IAuthenticationService authService)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _authService = authService;
    }

    public async Task<VehicleMappingResponse> GetVehicleMappingsAsync()
    {
        var accessToken = await _authService.GetAccessTokenAsync();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{_options.ApiBaseUrl}/customers/vehicles/mappings");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Headers.Add("x-version", "v1");

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        var mappings = await response.Content.ReadFromJsonAsync<VehicleMapping[]>();
        return new VehicleMappingResponse(mappings ?? []);
    }

    public async Task<TelematicDataResponse> GetTelematicDataAsync(string vin, string containerId)
    {
        var accessToken = await _authService.GetAccessTokenAsync();

        var url = $"{_options.ApiBaseUrl}/customers/vehicles/{vin}/telematicData?containerId={Uri.EscapeDataString(containerId)}";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Headers.Add("x-version", "v1");

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<TelematicDataResponse>())!;
    }
}
