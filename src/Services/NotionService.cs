using System.Text.Json;
using AutoMapper;
using Markdig;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notion.Client;
using NotionBae.Utilities;

namespace NotionBae.Services;

public interface INotionService
{
    Task<HttpResponseMessage> Search(string query);
    Task<Page> CreatePage(string parentId, string title, string content);
    Task<HttpResponseMessage> RetrievePage(string pageId);
    Task<HttpResponseMessage> RetrieveBlockChildren(string blockId);
    Task<HttpResponseMessage> UpdatePage(string pageId, string title);
    Task<HttpResponseMessage> UpdateBlock(string blockId, string content);
    Task<HttpResponseMessage> DeleteBlock(string blockId);
    Task DeleteBlocks(List<string> blockIds);
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
    private readonly IMapper _mapper;
    private readonly NotionClient _client;

    public NotionService(HttpClient httpclient, IConfiguration configuration, ILogger<NotionService> logger, IMapper mapper)
    {
        var token = configuration["NotionApiKey"];
        _httpclient = httpclient;
        _httpclient.BaseAddress = new Uri("https://api.notion.com/v1/");
        _httpclient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");
        _httpclient.DefaultRequestHeaders.Remove("Authorization");
        _httpclient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        _logger = logger;
        _mapper = mapper;
        
        //TODO fix this:
        _client = NotionClientFactory.Create(new ClientOptions
        {
            AuthToken = token
        });
    }
    
    public async Task<HttpResponseMessage> Search(string query)
    {
        var payload = new { query };
        return await SendPostRequestAsync("search", payload);
    }
    
    public async Task<Page> CreatePage(string parentId, string title, string content)
    {
        // var blocks = MarkdownToNotionConverter.ConvertToNotionBlocks(content);
        var documents = Markdown.Parse(content);
        
        var blocks = _mapper.Map<List<IBlock>>(documents, opt => opt.Items["AllBlocks"] = new List<Block>());

        var pagesCreateParameters = PagesCreateParametersBuilder
            .Create(new ParentPageInput
            {
                PageId = parentId
            })
            .AddProperty("title", new TitlePropertyValue
            {
                Title = new List<RichTextBase> {new RichTextText {Text = new Text {Content = "Test"}}}
            });
        
        var page = pagesCreateParameters.Build();
        page.Children = blocks;

        return await _client.Pages.CreateAsync(page);
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
        
        
        return await SendPatchRequestAsync($"blocks/{blockId}", blocks.FirstOrDefault());
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

    public async Task DeleteBlocks(List<string> blockIds)
    {
        var tasks = new List<Task>();
        foreach (var blockId in blockIds)
        {
            tasks.Add(Task.Run(async () => await DeleteBlock(blockId)));
        }

        try
        {
            await Task.WhenAll(tasks);
            _logger.LogInformation("Successfully deleted existing blocks");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception occurred while deleting existing blocks");
            throw;
        }
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
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        
        var payloadJson = JsonSerializer.Serialize(payload, options);
        
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
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        
        var payloadJson = JsonSerializer.Serialize(payload, options);
        
        _logger.LogInformation("Sending {Endpoint} request with payload: {Payload}", endpoint, payloadJson);
        
        var jsonContent = new StringContent(
            payloadJson,
            System.Text.Encoding.UTF8,
            "application/json");
            
        return await _httpclient.PostAsync(endpoint, jsonContent);
    }
}