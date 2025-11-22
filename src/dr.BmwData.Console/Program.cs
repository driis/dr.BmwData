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
builder.Services.AddHttpClient<IAuthenticationService, AuthenticationService>();
builder.Services.AddSingleton<BmwClient>();
builder.Services.AddTransient<BmwConsoleApp>();

var host = builder.Build();

// Application Logic
using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;

try
{
    await services.GetRequiredService<BmwConsoleApp>().RunAsync(CancellationToken.None);
}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred.");
}
