using System.Net.Http.Json;
using dr.BmwData.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace dr.BmwData;

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly BmwOptions _options;
    private CodeChallenge? _challenge = null;

    public AuthenticationService(HttpClient httpClient, IOptions<BmwOptions> options, ILogger<AuthenticationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<DeviceCodeResponse> InitiateDeviceFlowAsync(string scope)
    {
        _challenge = new();
        var request = new DeviceCodeRequest(_options.ClientId, scope, _challenge.Challenge);
        var content = ToFormUrlEncodedContent(request);
        
        var response = await _httpClient.PostAsync($"{_options.DeviceFlowBaseUrl}/gcdm/oauth/device/code", content);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<DeviceCodeResponse>())!;
    }

    public async Task<TokenResponse> PollForTokenAsync(string clientId, string deviceCode, int interval, int expiresIn)
    {
        if ( _challenge == null)
        {
            throw new InvalidOperationException("Call InitiaiteDeviceFlowAsync before polling for ");
        }
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(expiresIn))
        {
            await Task.Delay(interval * 1000);

            var request = new TokenRequest(clientId, deviceCode, _challenge.Verification);
            var content = ToFormUrlEncodedContent(request);

            var response = await _httpClient.PostAsync($"{_options.DeviceFlowBaseUrl}/gcdm/oauth/token", content);

            if (response.IsSuccessStatusCode)
            {
                return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            if (errorContent.Contains("authorization_pending"))
            {
                _logger.LogInformation("Authorization pending. Waiting...");
                continue;
            }

            if (errorContent.Contains("slow_down"))
            {
                _logger.LogInformation("Slow down received. Increasing interval.");
                interval += 5;
                continue;
            }

            _logger.LogError($"Token polling failed: {errorContent}");
            throw new Exception($"Token polling failed: {errorContent}");
        }

        throw new TimeoutException("Device code flow timed out.");
    }

    private static FormUrlEncodedContent ToFormUrlEncodedContent<T>(T request)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var dictionary = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        return new FormUrlEncodedContent(dictionary!);
    }
}