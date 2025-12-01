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
    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _tokenExpiresAt = DateTime.MinValue;

    public AuthenticationService(HttpClient httpClient, IOptions<BmwOptions> options, ILogger<AuthenticationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        _refreshToken = string.IsNullOrEmpty(_options.RefreshToken) ? null : _options.RefreshToken;
    }

    public bool RequiresInteractiveFlow => !HasValidAuthenticationToken && !HasRefreshToken;

    private bool HasRefreshToken => !string.IsNullOrEmpty(_refreshToken);

    private bool HasValidAuthenticationToken => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiresAt;

    public async Task<string> GetAccessTokenAsync()
    {
        // If we have a valid (non-expired) access token, return it
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiresAt)
        {
            return _accessToken;
        }

        // If we have a refresh token, use it to get a new access token
        if (!string.IsNullOrEmpty(_refreshToken))
        {
            _logger.LogInformation("Access token expired or missing. Refreshing token...");
            await RefreshTokenAsync(_refreshToken);
            return _accessToken!;
        }

        // No token and no refresh token - throw with instructions
        throw new InvalidOperationException(
            "No valid access token available and no refresh token configured. " +
            "Please run the interactive device flow first by calling InitiateDeviceFlowAsync() " +
            "followed by PollForTokenAsync(), or configure a refresh token in BmwOptions.");
    }

    private void StoreToken(TokenResponse tokenResponse)
    {
        _accessToken = tokenResponse.AccessToken;
        _refreshToken = tokenResponse.RefreshToken;
        // Subtract 60 seconds as a buffer to refresh before actual expiration
        _tokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60);
    }

    public async Task<DeviceCodeResponse> InitiateDeviceFlowAsync(string scope)
    {
        if (string.IsNullOrEmpty(_options.ClientId))
        {
            throw new InvalidOperationException(
                "ClientId is not configured. Please configure BmwOptions.ClientId in appsettings.json " +
                "or via environment variable BmwData__ClientId.");
        }

        _challenge = new();
        var request = new DeviceCodeRequest(_options.ClientId, scope, _challenge.Challenge);
        var content = ToFormUrlEncodedContent(request);
        
        var response = await _httpClient.PostAsync($"{_options.DeviceFlowBaseUrl}/gcdm/oauth/device/code", content);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<DeviceCodeResponse>())!;
    }

    public async Task PollForTokenAsync(DeviceCodeResponse deviceCode)
    {
        if ( _challenge == null)
        {
            throw new InvalidOperationException("Call InitiaiteDeviceFlowAsync before polling for ");
        }

        // Use configurable intervals from options (in milliseconds)
        var intervalMs = deviceCode.Interval * 1000;

        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(deviceCode.ExpiresIn))
        {
            await Task.Delay(intervalMs);

            var request = new TokenRequest(_options.ClientId, deviceCode.DeviceCode, _challenge.Verification);
            var content = ToFormUrlEncodedContent(request);

            var response = await _httpClient.PostAsync($"{_options.DeviceFlowBaseUrl}/gcdm/oauth/token", content);

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
                StoreToken(tokenResponse);
                return;
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
                intervalMs += _options.SlowDownIncrementMs;
                continue;
            }

            _logger.LogError($"Token polling failed: {errorContent}");
            throw new Exception($"Token polling failed: {errorContent}");
        }

        throw new TimeoutException("Device code flow timed out.");
    }

    private async Task RefreshTokenAsync(string refreshToken)
    {
        var request = new RefreshTokenRequest(_options.ClientId, refreshToken);
        var content = ToFormUrlEncodedContent(request);

        var response = await _httpClient.PostAsync($"{_options.DeviceFlowBaseUrl}/gcdm/oauth/token", content);

        response.EnsureSuccessStatusCode();

        var tokenResponse = (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
        StoreToken(tokenResponse);
    }

    private static FormUrlEncodedContent ToFormUrlEncodedContent<T>(T request)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var dictionary = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        return new FormUrlEncodedContent(dictionary!);
    }
}