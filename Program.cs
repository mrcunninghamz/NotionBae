using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotionBae.Services;

// Create the application builder
var builder = Host.CreateApplicationBuilder(args);

// Configure to use environmental variables then to use appsettings.local.json
builder.Configuration
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);


// Configure logging
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Add the MCP server
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

// Register HttpClient for NotionService
builder.Services.AddHttpClient<INotionService, NotionService>();

// Build and run the host
var host = builder.Build();

// You can access configuration values like this:
var config = host.Services.GetRequiredService<IConfiguration>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Configuration loaded. Any settings from appsettings.local.json are now available.");

// Example of reading a configuration value (if it exists)
if (config["NotionApiKey"] == null)
{
    logger.LogWarning("NotionApiKey not found in configuration sources. Please set it in appsettings.local.json or as an environment variable.");
    throw new Exception("NotionApiKey not found in configuration. The application requires a valid Notion API key to function.");   
}
else
{
    logger.LogInformation("NotionApiKey found in configuration.");
}


await host.RunAsync();