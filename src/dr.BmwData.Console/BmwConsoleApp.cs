using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static System.Console;

using dr.BmwData;

namespace dr.BmwData.Console;

public class BmwConsoleApp(IOptions<BmwOptions> options, IAuthenticationService authService, ILogger<BmwConsoleApp> logger)
{
    public BmwOptions Options { get; } = options.Value;

    public async Task RunAsync(CancellationToken ct)
    {
        logger.LogInformation("BMW Open Car Data Client Demo");
        logger.LogInformation($"ClientId: {Options.ClientId}");

        // Check if interactive authentication is needed
        if (authService.RequiresInteractiveFlow)
        {
            try
            {
                WriteLine("No valid token available. Initiating device code flow...");
                var deviceCodeResponse = await authService.InitiateDeviceFlowAsync();

                WriteLine($"Please visit: {deviceCodeResponse.VerificationUri}");
                WriteLine($"And enter code: {deviceCodeResponse.UserCode}");

                logger.LogInformation("Polling for token...");
                await authService.PollForTokenAsync(deviceCodeResponse);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Authentication failed.");
                return;
            }
        }

        // Get access token (will use cached token or refresh automatically)
        try
        {
            var accessToken = await authService.GetAccessTokenAsync();
            logger.LogInformation("Successfully authenticated!");
            logger.LogDebug($"Access Token: {accessToken[..20]}...");

            // TODO: Use ContainerService or other services that now handle tokens internally
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get access token.");
        }
    }
}