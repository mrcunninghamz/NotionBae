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
    
    [McpServerTool(Name = "nb_get_page_content"), Description("Retrieves a Notion page with its metadata and full content in markdown format.")]
    public async Task<string> GetPageContent(string pageId)
    {
        _logger.LogInformation("Retrieving Notion page content with ID: {PageId}", pageId);
        
        try
        {
            // Get page metadata
            var pageResponse = await _notionService.RetrievePage(pageId);
            
            if (!pageResponse.IsSuccessStatusCode)
            {
                var errorContent = await pageResponse.Content.ReadAsStringAsync();
                var detailedError = NotionResponseHelper.ExtractErrorMessage(errorContent);
                
                _logger.LogError("Error retrieving Notion page metadata: {StatusCode} with message: {Message}", 
                    pageResponse.StatusCode, detailedError);
                return $"Error retrieving Notion page metadata: {pageResponse.StatusCode}\nDetails: {detailedError}";
            }

            var pageResponseContent = await pageResponse.Content.ReadAsStringAsync();
            var pageResult = JsonDocument.Parse(pageResponseContent);
            
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
            
            // Get page content
            var contentResponse = await _notionService.RetrieveBlockChildren(pageId);
            
            if (!contentResponse.IsSuccessStatusCode)
            {
                var errorContent = await contentResponse.Content.ReadAsStringAsync();
                var detailedError = NotionResponseHelper.ExtractErrorMessage(errorContent);
                
                _logger.LogError("Error retrieving Notion page content: {StatusCode} with message: {Message}", 
                    contentResponse.StatusCode, detailedError);
                return $"Error retrieving Notion page content: {contentResponse.StatusCode}\nDetails: {detailedError}";
            }

            var contentResponseContent = await contentResponse.Content.ReadAsStringAsync();
            var blockResult = JsonDocument.Parse(contentResponseContent);
            
            string markdown = "No content found";
            if (blockResult.RootElement.TryGetProperty("results", out var results))
            {
                markdown = NotionToMarkdownConverter.ConvertToMarkdown(results);
            }
            
            // Combine metadata and content into a single response
            var response = $"""
                # {title}
                
                ## Page Information
                - **ID**: {pageId}
                - **Private URL**: {privateUrl}
                - **Public URL**: {publicUrl}
                
                ## Page Content
                
                {markdown}
                """;
            
            _logger.LogInformation("Successfully retrieved page content for ID: {PageId}", pageId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving Notion page content: {Message}", ex.Message);
            return $"Exception occurred while retrieving Notion page content: {ex.Message}";
        }
    }
}