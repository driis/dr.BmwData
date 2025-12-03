using System.Text.Json;
using Microsoft.Extensions.Logging;
using static System.Console;

using dr.BmwData;

namespace dr.BmwData.Console;

public class BmwConsoleApp(
    IAuthenticationService authService,
    IContainerService containerService,
    ILogger<BmwConsoleApp> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task RunAsync(CommandLineArgs args, CancellationToken ct)
    {
        // Handle help command immediately (no authentication needed)
        if (args.Command == Command.Help)
        {
            CommandLineArgs.PrintHelp();
            return;
        }

        // Ensure authentication for all other commands
        if (!await EnsureAuthenticatedAsync())
            return;

        // Execute the requested command
        await ExecuteCommandAsync(args);
    }

    private async Task<bool> EnsureAuthenticatedAsync()
    {
        if (await authService.RequiresInteractiveFlowAsync())
        {
            try
            {
                WriteLine("No valid token available. Initiating device code flow...");
                var deviceCodeResponse = await authService.InitiateDeviceFlowAsync();

                WriteLine();
                WriteLine($"Please visit: {deviceCodeResponse.VerificationUri}");
                WriteLine($"And enter code: {deviceCodeResponse.UserCode}");
                WriteLine();

                logger.LogInformation("Polling for token...");
                await authService.PollForTokenAsync(deviceCodeResponse);
                WriteLine("Authentication successful!");
                WriteLine("Refresh token has been saved automatically.");
                WriteLine();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Authentication failed.");
                WriteLine($"Authentication failed: {ex.Message}");
                return false;
            }
        }

        return true;
    }

    private async Task ExecuteCommandAsync(CommandLineArgs args)
    {
        try
        {
            switch (args.Command)
            {
                case Command.List:
                    await ListContainersAsync();
                    break;
                case Command.Create:
                    await CreateContainerAsync(args.TechnicalDescriptors!);
                    break;
                case Command.Get:
                    await GetContainerAsync(args.ContainerId!);
                    break;
                case Command.Delete:
                    await DeleteContainerAsync(args.ContainerId!);
                    break;
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "API request failed.");
            WriteLine($"Error: {ex.Message}");
        }
    }

    private async Task ListContainersAsync()
    {
        WriteLine("Fetching containers...");
        var response = await containerService.ListContainersAsync();

        if (response.Containers.Length == 0)
        {
            WriteLine("No containers found.");
            return;
        }

        WriteLine($"Found {response.Containers.Length} container(s):");
        WriteLine();

        foreach (var container in response.Containers)
        {
            WriteLine($"  ID:      {container.ContainerId}");
            WriteLine($"  Name:    {container.Name}");
            WriteLine($"  Purpose: {container.Purpose}");
            WriteLine($"  State:   {container.State}");
            WriteLine($"  Created: {container.Created:yyyy-MM-dd HH:mm:ss}");
            WriteLine();
        }
    }

    private async Task CreateContainerAsync(string[] technicalDescriptors)
    {
        WriteLine($"Creating container with descriptors: {string.Join(", ", technicalDescriptors)}");
        var response = await containerService.CreateContainerAsync(technicalDescriptors);

        WriteLine();
        WriteLine("Container created successfully!");
        WriteLine($"  ID:      {response.ContainerId}");
        WriteLine($"  Name:    {response.Name}");
        WriteLine($"  Purpose: {response.Purpose}");
        WriteLine($"  State:   {response.State}");
        WriteLine($"  Created: {response.Created:yyyy-MM-dd HH:mm:ss}");
    }

    private async Task GetContainerAsync(string containerId)
    {
        WriteLine($"Fetching container: {containerId}");
        var response = await containerService.GetContainerAsync(containerId);

        WriteLine();
        var json = JsonSerializer.Serialize(response, JsonOptions);
        WriteLine(json);
    }

    private async Task DeleteContainerAsync(string containerId)
    {
        WriteLine($"Deleting container: {containerId}");
        await containerService.DeleteContainerAsync(containerId);
        WriteLine("Container deleted successfully.");
    }
}