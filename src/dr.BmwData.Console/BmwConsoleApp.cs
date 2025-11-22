using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static System.Console;

using dr.BmwData;

namespace dr.BmwData.Console;

public class BmwConsoleApp(IOptions<BmwOptions> options, BmwClient client, IAuthenticationService authService, ILogger<BmwConsoleApp> logger)
{
    public BmwOptions Options { get; } = options.Value;

    public async Task RunAsync(CancellationToken ct)
    {
        logger.LogInformation("BMW Open Car Data Client Demo");
        logger.LogInformation($"Base URL: {Options.BaseUrl}");

        try
        {
            logger.LogInformation("Initiating device code flow...");
            var deviceCodeResponse = await authService.InitiateDeviceFlowAsync(Options.ClientId, "authenticate_user openid cardata:api:read cardata:streaming:read");

            WriteLine($"Please visit: {deviceCodeResponse.VerificationUri}");
            WriteLine($"And enter code: {deviceCodeResponse.UserCode}");

            logger.LogInformation("Polling for token...");
            var tokenResponse = await authService.PollForTokenAsync(Options.ClientId, deviceCodeResponse.DeviceCode, deviceCodeResponse.Interval, deviceCodeResponse.ExpiresIn);

            logger.LogInformation($"Successfully authenticated! Access Token: {tokenResponse.AccessToken}");

            var vin = "WBA1234567890"; // Mock VIN
            
            // TODO: Pass token to client
            var telemetry = await client.GetTelemetryAsync(vin);

            logger.LogInformation($"Telemetry received:");
            logger.LogInformation($"Fuel Level: {telemetry.FuelLevel} %");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during execution.");
        }
    }
}