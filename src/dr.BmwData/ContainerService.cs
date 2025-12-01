using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using dr.BmwData.Models;
using Microsoft.Extensions.Options;

namespace dr.BmwData;

public class ContainerService : IContainerService
{
    private readonly HttpClient _httpClient;
    private readonly BmwOptions _options;
    private readonly IAuthenticationService _authService;

    public ContainerService(HttpClient httpClient, IOptions<BmwOptions> options, IAuthenticationService authService)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _authService = authService;
    }

    public async Task<ContainerResponse> CreateContainerAsync(string[] technicalDescriptors)
    {
        var accessToken = await _authService.GetAccessTokenAsync();

        var request = new CreateContainerRequest(
            Name: "CarData Container",
            Purpose: "Telemetry data collection",
            TechnicalDescriptors: technicalDescriptors);

        var jsonContent = JsonSerializer.Serialize(request);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.ApiBaseUrl}/customers/containers");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Headers.Add("x-version", "v1");
        httpRequest.Content = content;

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ContainerResponse>())!;
    }

    public async Task<ContainerListResponse> ListContainersAsync()
    {
        var accessToken = await _authService.GetAccessTokenAsync();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{_options.ApiBaseUrl}/customers/containers");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Headers.Add("x-version", "v1");

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ContainerListResponse>())!;
    }

    public async Task<ContainerResponse> GetContainerAsync(string containerId)
    {
        var accessToken = await _authService.GetAccessTokenAsync();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{_options.ApiBaseUrl}/customers/containers/{containerId}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Headers.Add("x-version", "v1");

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ContainerResponse>())!;
    }

    public async Task DeleteContainerAsync(string containerId)
    {
        var accessToken = await _authService.GetAccessTokenAsync();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"{_options.ApiBaseUrl}/customers/containers/{containerId}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Headers.Add("x-version", "v1");

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();
    }
}
