using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace dr.BmwData.Console;

public class BmwConsoleApp(IOptions<BmwOptions> options, BmwClient client, ILogger<BmwConsoleApp> logger)
{
    public BmwOptions Options { get; } = options.Value;

    public async Task RunAsync(CancellationToken ct)
    {
        logger.LogInformation("BMW Open Car Data Client Demo");
        logger.LogInformation($"Base URL: {Options.BaseUrl}"); // Demonstrate config reading

        var vin = "WBA1234567890"; // Mock VIN

        
        var telemetry = await client.GetTelemetryAsync(vin);

        logger.LogInformation($"Telemetry received:");
        logger.LogInformation($"Fuel Level: {telemetry.FuelLevel} %");
    }
}