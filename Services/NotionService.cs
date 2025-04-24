using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotionBae.Utilities;

namespace NotionBae.Services;

public interface INotionService
{
    Task<HttpResponseMessage> Search(string query);
    Task<HttpResponseMessage> CreatePage(string parentId, string title, string description, string content);
    Task<HttpResponseMessage> RetrievePage(string pageId);
    Task<HttpResponseMessage> RetrieveBlockChildren(string blockId);
}

public class NotionService : INotionService
{
    private readonly HttpClient _httpclient;
    private readonly ILogger<NotionService> _logger;

    public NotionService(HttpClient httpclient, IConfiguration configuration, ILogger<NotionService> logger)
    {
        var token = configuration["NotionApiKey"];
        _httpclient = httpclient;
        _httpclient.BaseAddress = new Uri("https://api.notion.com/v1/");
        _httpclient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");
        _httpclient.DefaultRequestHeaders.Remove("Authorization");
        _httpclient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        _logger = logger;
    }
    
    public async Task<HttpResponseMessage> Search(string query)
    {
        var payload = new { query };
        return await SendPostRequestAsync("search", payload);
    }
    
    public async Task<HttpResponseMessage> CreatePage(string parentId, string title, string description, string content)
    {
        var blocks = MarkdownToNotionConverter.ConvertToNotionBlocks(content);
        var payload = new
        {
            parent = new
            {
                page_id = parentId
            },
            properties = new
            {
                title = new object[]
                {
                    new
                    {
                        text = new
                        {
                            content = title
                        }
                    }
                }
            },
            children = blocks
        };
        
        return await SendPostRequestAsync("pages", payload);
    }
    
    private async Task<HttpResponseMessage> SendPostRequestAsync<T>(string endpoint, T payload)
    {
        var payloadJson = JsonSerializer.Serialize(payload);
        
        _logger.LogInformation("Sending {Endpoint} request with payload: {Payload}", endpoint, payloadJson);
        
        var jsonContent = new StringContent(
            payloadJson,
            System.Text.Encoding.UTF8,
            "application/json");
            
        return await _httpclient.PostAsync(endpoint, jsonContent);
    }

    public async Task<HttpResponseMessage> RetrievePage(string pageId)
    {
        return await _httpclient.GetAsync($"pages/{pageId}");
    }

    public async Task<HttpResponseMessage> RetrieveBlockChildren(string blockId)
    {
        return await _httpclient.GetAsync($"blocks/{blockId}/children");
    }
}