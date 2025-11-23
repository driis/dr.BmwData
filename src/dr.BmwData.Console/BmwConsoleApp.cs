using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static System.Console;

using dr.BmwData;
using dr.BmwData.Models;

namespace dr.BmwData.Console;

public class BmwConsoleApp(IOptions<BmwOptions> options, BmwClient client, IAuthenticationService authService, ILogger<BmwConsoleApp> logger)
{
    public BmwOptions Options { get; } = options.Value;

    public async Task RunAsync(CancellationToken ct)
    {
        logger.LogInformation("BMW Open Car Data Client Demo");
        logger.LogInformation($"ClientId: {Options.ClientId}");

        TokenResponse? tokenResponse = null;

        // 1. Try Refresh Token
        if (!string.IsNullOrEmpty(Options.RefreshToken))
        {
            try
            {
                logger.LogInformation("Found configured refresh token. Attempting to refresh...");
                tokenResponse = await authService.RefreshTokenAsync(Options.RefreshToken);
                logger.LogInformation("Token refresh successful!");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Token refresh failed: {ex.Message}. Falling back to interactive login.");
            }
        }

        // 2. Fallback to Device Code Flow
        if (tokenResponse == null)
        {
            try
            {
                logger.LogInformation("Initiating device code flow...");
                var deviceCodeResponse = await authService.InitiateDeviceFlowAsync();

                WriteLine($"Please visit: {deviceCodeResponse.VerificationUri}");
                WriteLine($"And enter code: {deviceCodeResponse.UserCode}");

                logger.LogInformation("Polling for token...");
                tokenResponse = await authService.PollForTokenAsync(Options.ClientId, deviceCodeResponse.DeviceCode, deviceCodeResponse.Interval, deviceCodeResponse.ExpiresIn);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Authentication failed.");
                return;
            }
        }

        if (tokenResponse != null)
        {
            logger.LogInformation($"Successfully authenticated!");
            logger.LogInformation($"Access Token: {tokenResponse.AccessToken}");
            logger.LogInformation($"Refresh Token: {tokenResponse.RefreshToken}");
            
            // TODO: Persist the new refresh token if needed, or just output it for now as requested.

            var vin = "WBA1234567890"; // Mock VIN
            
            // TODO: Pass token to client
            // var telemetry = await client.GetTelemetryAsync(vin);

            // logger.LogInformation($"Telemetry received:");
            // logger.LogInformation($"Fuel Level: {telemetry.FuelLevel} %");
        }
    }
}