using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotionBae.Utilities;

namespace NotionBae.Services;

public interface INotionService
{
    Task<HttpResponseMessage> Search(string query);
    Task<HttpResponseMessage> CreatePage(string parentId, string title, string content);
    Task<HttpResponseMessage> RetrievePage(string pageId);
    Task<HttpResponseMessage> RetrieveBlockChildren(string blockId);
    Task<HttpResponseMessage> UpdatePage(string pageId, string title);
    Task<HttpResponseMessage> UpdateBlock(string blockId, string content);
    Task<HttpResponseMessage> DeleteBlock(string blockId);
    Task<HttpResponseMessage> AppendBlockChildren(string blockId, string content, string? after = null);
}

public class NotionPageUpdateRequest
{
    public TitleProperty? Properties { get; init; }
    public List<object>? Children { get; init; }
}

public class TitleProperty
{
    public required TitleElement[] Title { get; init; }
}

public class TitleElement
{
    public required TextContent Text { get; init; }
}

public class TextContent
{
    public required string Content { get; init; }
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
    
    public async Task<HttpResponseMessage> CreatePage(string parentId, string title, string content)
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

    public async Task<HttpResponseMessage> RetrievePage(string pageId)
    {
        return await _httpclient.GetAsync($"pages/{pageId}");
    }

    public async Task<HttpResponseMessage> RetrieveBlockChildren(string blockId)
    {
        return await _httpclient.GetAsync($"blocks/{blockId}/children");
    }

    public async Task<HttpResponseMessage> UpdatePage(string pageId, string title)
    {
        var properties = CreateTitleProperties(title);
        var payload = CreateUpdatePayload(properties);
        
        return await SendPatchRequestAsync($"pages/{pageId}", payload);
    }
    
    public async Task<HttpResponseMessage> UpdateBlock(string blockId, string content)
    {
        var blocks = !string.IsNullOrEmpty(content) ? CreateContentBlocks(content) : null;
        
        var payload = new
        {
            children = blocks ?? new List<object>()
        };
        
        return await SendPatchRequestAsync($"blocks/{blockId}", payload);
    }

    public async Task<HttpResponseMessage> DeleteBlock(string blockId)
    {
        _logger.LogInformation("Deleting block with ID: {BlockId}", blockId);
        
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Delete,
            RequestUri = new Uri(_httpclient.BaseAddress, $"blocks/{blockId}")
        };
        
        return await _httpclient.SendAsync(request);
    }
    
    public async Task<HttpResponseMessage> AppendBlockChildren(string blockId, string content, string? after = null)
    {
        _logger.LogInformation("Appending children to block with ID: {BlockId}", blockId);
        
        var blocks = !string.IsNullOrEmpty(content) ? MarkdownToNotionConverter.ConvertToNotionBlocks(content) : null;
        var payload = new Dictionary<string, object>
        {
            ["children"] = blocks ?? new List<object>()
        };
        
        if (!string.IsNullOrEmpty(after))
        {
            payload["after"] = after;
        }
        
        return await SendPatchRequestAsync($"blocks/{blockId}/children", payload);
    }
    
    private static TitleProperty? CreateTitleProperties(string title) =>
        !string.IsNullOrEmpty(title)
            ? new TitleProperty
            {
                Title = new[]
                {
                    new TitleElement
                    {
                        Text = new TextContent { Content = title }
                    }
                }
            }
            : null;

    private static List<object>? CreateContentBlocks(string content) =>
        !string.IsNullOrEmpty(content)
            ? MarkdownToNotionConverter.ConvertToNotionBlocks(content)
            : null;

    private static NotionPageUpdateRequest CreateUpdatePayload(TitleProperty? properties, List<object>? blocks = null) =>
        new()
        {
            Properties = properties,
            Children = blocks
        };
    
    private async Task<HttpResponseMessage> SendPatchRequestAsync<T>(string endpoint, T payload)
    {
        var payloadJson = JsonSerializer.Serialize(payload);
        
        _logger.LogInformation("Sending PATCH {Endpoint} request with payload: {Payload}", endpoint, payloadJson);
        
        var jsonContent = new StringContent(
            payloadJson,
            System.Text.Encoding.UTF8,
            "application/json");
            
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Patch,
            RequestUri = new Uri(_httpclient.BaseAddress, endpoint),
            Content = jsonContent
        };
            
        return await _httpclient.SendAsync(request);
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
}