using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notion.Client;
using NotionBae.Services;
using Polly;
using Polly.Extensions.Http;

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

builder.Services.AddAutoMapper(typeof(Program));

// Register HttpClient for NotionService

builder.Services.AddHttpClient<IRestClient, NotionBaeRestClient>()
    .AddPolicyHandler(Policy.BulkheadAsync<HttpResponseMessage>(10, Int32.MaxValue))
    .AddPolicyHandler((provider, _) => GetRetryOnRateLimitingPolicy(provider));

builder.Services.AddSingleton<IUsersClient, UsersClient>();
builder.Services.AddSingleton<IDatabasesClient, DatabasesClient>();
builder.Services.AddSingleton<IPagesClient, PagesClient>();
builder.Services.AddSingleton<ISearchClient, SearchClient>();
builder.Services.AddSingleton<ICommentsClient, CommentsClient>();
builder.Services.AddSingleton<IBlocksClient, BlocksClient>();
builder.Services.AddSingleton<IAuthenticationClient, AuthenticationClient>();
builder.Services.AddSingleton<INotionClient, NotionClient>();
// builder.Services.AddNotionClient(op =>
// {
// });
builder.Services.AddSingleton<INotionService, NotionService>();

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
return;

static IAsyncPolicy<HttpResponseMessage> GetRetryOnRateLimitingPolicy(IServiceProvider provider)
{
    var logger = provider.GetService<ILogger<Program>>();
    // Retry when these status codes are encountered.
    HttpStatusCode[] httpStatusCodesWorthRetrying = {
        HttpStatusCode.InternalServerError, // 500
        HttpStatusCode.BadGateway, // 502
        HttpStatusCode.GatewayTimeout // 504
    };
    // Define our waitAndRetry policy: retry n times with an exponential backoff in case the API throttles us for too many requests.
    var waitAndRetryPolicy = Policy
        .HandleResult<HttpResponseMessage> (e => e.StatusCode == HttpStatusCode.ServiceUnavailable 
                                                 || e.StatusCode == (System.Net.HttpStatusCode) 429
                                                 || e.StatusCode == (System.Net.HttpStatusCode) 409)
        .WaitAndRetryAsync(10, // Retry 10 times with a delay between retries before ultimately giving up
            attempt => TimeSpan.FromSeconds(0.25 * Math.Pow(2, attempt)), // Back off!  2, 4, 8, 16 etc times 1/4-second
            //attempt => TimeSpan.FromSeconds(6), // Wait 6 seconds between retries
            (exception, calculatedWaitDuration) => {
                logger.LogInformation($"API server is throttling our requests. Automatically delaying for {calculatedWaitDuration.TotalMilliseconds}ms");
            }
        );

    // Define our first CircuitBreaker policy: Break if the action fails 4 times in a row.
    // a number of recoverable status messages, such as 500, 502, and 504.
    var circuitBreakerPolicyForRecoverable = HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode))
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(3),
            onBreak: (outcome, breakDelay) => {
                logger.LogInformation($"Polly Circuit Breaker logging: Breaking the circuit for {breakDelay.TotalMilliseconds}ms due to: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
            },
            onReset: () => logger.LogInformation("Polly Circuit Breaker logging: Call ok... closed the circuit again"),
            onHalfOpen: () => logger.LogInformation("Polly Circuit Breaker logging: Half-open: Next call is a trial")
        );

    // Combine the waitAndRetryPolicy and circuit breaker policy into a PolicyWrap. This defines our resiliency strategy.
    return Policy.WrapAsync(waitAndRetryPolicy, circuitBreakerPolicyForRecoverable);
}