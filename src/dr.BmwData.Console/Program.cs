using dr.BmwData;
using dr.BmwData.Console;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Services
builder.Services.Configure<BmwOptions>(builder.Configuration.GetSection(BmwOptions.SectionName));
builder.Services.AddHttpClient<AuthenticationService>();
builder.Services.AddSingleton<IAuthenticationService>(sp => sp.GetRequiredService<AuthenticationService>());
builder.Services.AddHttpClient<IContainerService, ContainerService>();
builder.Services.AddTransient<BmwConsoleApp>();

// Parse command-line arguments
CommandLineArgs parsedArgs;
try
{
    parsedArgs = CommandLineArgs.Parse(args);
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine();
    CommandLineArgs.PrintHelp();
    return 1;
}

var host = builder.Build();

// Application Logic
using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;

try
{
    await services.GetRequiredService<BmwConsoleApp>().RunAsync(parsedArgs, CancellationToken.None);
    return 0;
}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred.");
    return 1;
}
