using System.Net.Http.Json;
using dr.BmwData.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Options;

namespace dr.BmwData;

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly BmwOptions _options;
    private string _codeVerifier;
    private CodeChallenge _challenge = new CodeChallenge();
    
    public AuthenticationService(HttpClient httpClient, IOptions<BmwOptions> options, ILogger<AuthenticationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<DeviceCodeResponse> InitiateDeviceFlowAsync(string clientId, string scope)
    {
        var codeChallenge = _challenge.Challenge;
        var request = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "response_type", "device_code" },
            { "scope", scope },
            { "code_challenge", codeChallenge },
            { "code_challenge_method", "S256" }
        };

        var content = new FormUrlEncodedContent(request);
        var response = await _httpClient.PostAsync($"{_options.DeviceFlowBaseUrl}/gcdm/oauth/device/code", content);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DeviceCodeResponse>();
    }

    public async Task<TokenResponse> PollForTokenAsync(string clientId, string deviceCode, int interval, int expiresIn)
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(expiresIn))
        {
            await Task.Delay(interval * 1000);

            var request = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "grant_type", "urn:ietf:params:oauth:grant-type:device_code" },
                { "device_code", deviceCode },
                { "code_verifier", _challenge.Verification }
            };

            var content = new FormUrlEncodedContent(request);
            var response = await _httpClient.PostAsync($"{_options.DeviceFlowBaseUrl}/gcdm/oauth/token", content);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TokenResponse>();
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


