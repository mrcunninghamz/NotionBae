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
            var pageUrl = "";
            
            // Extract page ID
            if (pageResult.RootElement.TryGetProperty("id", out var idProp))
            {
                pageId = idProp.GetString() ?? "";
            }
            
            // Extract page URL
            if (pageResult.RootElement.TryGetProperty("url", out var urlProp))
            {
                pageUrl = urlProp.GetString() ?? "";
            }
            
            _logger.LogInformation("Page created successfully with ID: {PageId}", pageId);
            return $"Page created successfully!\nID: {pageId}\nURL: {pageUrl}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while creating Notion page: {Message}", ex.Message);
            return $"Exception occurred while creating Notion page: {ex.Message}";
        }
    }
}
