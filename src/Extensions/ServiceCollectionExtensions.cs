using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notion.Client;
using NotionBae.Services;

namespace NotionBae.Extensions;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddNotionClient(
        this IServiceCollection services, IConfiguration configuration)
    {
        
        // Register HttpClient for NotionService
        services.Configure<ClientOptions>(configuration.GetSection("Notion"));

        var httpClientBuilder = services.AddHttpClient<IRestClient, NotionBaeRestClient>((srv, httpClient) =>
        {
            var options = srv.GetService<IOptions<ClientOptions>>()?.Value ?? throw new Exception("Notion client options not configured");

            httpClient.BaseAddress = new Uri(options.BaseUrl);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.AuthToken);
            httpClient.DefaultRequestHeaders.Add("Notion-Version", options.NotionVersion);
            
        })
        .ConfigurePrimaryHttpMessageHandler(() => new LoggingHandler { InnerHandler = new HttpClientHandler() });

        services.AddSingleton<IUsersClient, UsersClient>();
        services.AddSingleton<IDatabasesClient, DatabasesClient>();
        services.AddSingleton<IPagesClient, PagesClient>();
        services.AddSingleton<ISearchClient, SearchClient>();
        services.AddSingleton<ICommentsClient, CommentsClient>();
        services.AddSingleton<IBlocksClient, BlocksClient>();
        services.AddSingleton<IAuthenticationClient, AuthenticationClient>();
        services.AddSingleton<INotionClient, NotionClient>();
        
        return httpClientBuilder;
    }
}
