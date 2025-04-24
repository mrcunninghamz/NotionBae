using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using NotionBae.Services;
using NotionBae.Utilities;

namespace NotionBae.Tools;

[McpServerToolType]
public class PageTool
{
    private readonly INotionService _notionService;
    private readonly ILogger<PageTool> _logger;

    public PageTool(INotionService notionService, ILogger<PageTool> logger)
    {
        _notionService = notionService;
        _logger = logger;
    }

    [McpServerTool(Name = "nb_create_page"), Description("Creates a new Notion page with the given title, description, and content.")]
    public async Task<string> CreatePage(
        string parentId, 
        string title, 
        string description, 
        string content)
    {
        _logger.LogInformation("Creating Notion page with title: {Title}, parentId: {ParentId}", title, parentId);
        
        try
        {
            var response = await _notionService.CreatePage(parentId, title, description, content);
        
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var detailedError = NotionResponseHelper.ExtractErrorMessage(errorContent);
                
                _logger.LogError("Error creating Notion page: {StatusCode} with message: {Message}", 
                    response.StatusCode, detailedError);
                return $"Error creating Notion page: {response.StatusCode}\nDetails: {detailedError}";
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var pageResult = JsonDocument.Parse(responseContent);
            
            var pageId = "";
            var privateUrl = "";
            var publicUrl = "";
            
            // Extract page ID
            if (pageResult.RootElement.TryGetProperty("id", out var idProp))
            {
                pageId = idProp.GetString() ?? "";
            }
            
            // Extract private URL
            if (pageResult.RootElement.TryGetProperty("url", out var urlProp))
            {
                privateUrl = urlProp.GetString() ?? "";
            }
            
            // Extract public URL
            if (pageResult.RootElement.TryGetProperty("public_url", out var publicUrlProp))
            {
                publicUrl = publicUrlProp.GetString() ?? "Not publicly shared";
            }
            
            _logger.LogInformation("Page created successfully with ID: {PageId}", pageId);
            return $"Page created successfully!\nID: {pageId}\nPrivate URL: {privateUrl}\nPublic URL: {publicUrl}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while creating Notion page: {Message}", ex.Message);
            return $"Exception occurred while creating Notion page: {ex.Message}";
        }
    }
    
    [McpServerTool(Name = "nb_get_page"), Description("Retrieves a Notion page by its ID.")]
    public async Task<string> GetPage(string pageId)
    {
        _logger.LogInformation("Retrieving Notion page with ID: {PageId}", pageId);
        
        try
        {
            var response = await _notionService.RetrievePage(pageId);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var detailedError = NotionResponseHelper.ExtractErrorMessage(errorContent);
                
                _logger.LogError("Error retrieving Notion page: {StatusCode} with message: {Message}", 
                    response.StatusCode, detailedError);
                return $"Error retrieving Notion page: {response.StatusCode}\nDetails: {detailedError}";
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var pageResult = JsonDocument.Parse(responseContent);
            
            var privateUrl = "";
            var publicUrl = "";
            var title = "";
            
            // Extract private URL
            if (pageResult.RootElement.TryGetProperty("url", out var urlProp))
            {
                privateUrl = urlProp.GetString() ?? "";
            }
            
            // Extract public URL
            if (pageResult.RootElement.TryGetProperty("public_url", out var publicUrlProp))
            {
                publicUrl = publicUrlProp.GetString() ?? "Not publicly shared";
            }
            
            // Extract page title
            if (pageResult.RootElement.TryGetProperty("properties", out var properties) &&
                properties.TryGetProperty("title", out var titleProp) &&
                titleProp.TryGetProperty("title", out var titleArray) &&
                titleArray.GetArrayLength() > 0)
            {
                var firstTitleElement = titleArray[0];
                if (firstTitleElement.TryGetProperty("plain_text", out var plainText))
                {
                    title = plainText.GetString() ?? "";
                }
            }
            
            _logger.LogInformation("Page retrieved successfully with ID: {PageId}", pageId);
            return $"Page retrieved successfully!\nID: {pageId}\nTitle: {title}\nPrivate URL: {privateUrl}\nPublic URL: {publicUrl}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving Notion page: {Message}", ex.Message);
            return $"Exception occurred while retrieving Notion page: {ex.Message}";
        }
    }
}