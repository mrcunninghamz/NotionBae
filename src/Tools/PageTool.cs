using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Dawn;
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

    [McpServerTool(Name = "nb_create_page"),
     Description("Creates a new Notion page with the given title, description, and content.")]
    public async Task<string> CreatePage(
        string parentId,
        string title,
        string content)
    {
        _logger.LogInformation("Creating Notion page with title: {Title}, parentId: {ParentId}", title, parentId);

        try
        {
            var page = await _notionService.CreatePage(parentId, title, content);
            
            var pageId = page.Id;
            var privateUrl = page.Url;
            var publicUrl = page.PublicUrl ?? "Not publicly shared";

            _logger.LogInformation("Page created successfully with ID: {PageId}", pageId);
            return $"Page created successfully!\nID: {pageId}\nPrivate URL: {privateUrl}\nPublic URL: {publicUrl}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while creating Notion page: {Message}", ex.Message);
            return $"Exception occurred while creating Notion page: {ex.Message}";
        }
    }

    [McpServerTool(Name = "nb_get_page_content"),
     Description("Retrieves a Notion page with its metadata and full content in markdown format. " +
                 "BlockId is in the comment after the Markdown element. " +
                 "A comment looks like this: `[//]: # (BlockId: 12345678-1234-1234-1234-123456789012)`")]
    public async Task<string> GetPageContent(
        string pageId)
    {
        _logger.LogInformation("Retrieving Notion page content with ID: {PageId}", pageId);

        try
        {
            // Get page metadata
            var pageResponse = await _notionService.RetrievePage(pageId);

            var privateUrl = pageResponse.Url ?? "";
            var publicUrl = pageResponse.PublicUrl ?? "Not publicly shared";


            // Get page content
            var content = await _notionService.GetPageContent(pageId);

            // Combine metadata and content into a single response
            var markdownResponse = $"""
                            ---
                            pageid: {pageId}
                            privateUrl: {privateUrl}
                            publicUrl: {publicUrl}
                            ---
                            {content}
                            """;

            _logger.LogInformation("Successfully retrieved page content for ID: {PageId}", pageId);
            return markdownResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving Notion page content: {Message}", ex.Message);
            return $"Exception occurred while retrieving Notion page content: {ex.Message}";
        }
    }

    [McpServerTool(Name = "nb_update_page"),
     Description("Updates an existing Notion page with new title and/or content.")]
    public async Task<string> UpdatePage(
        string pageId,
        string title = "")
    {
        _logger.LogInformation("Updating Notion page with ID: {PageId}", pageId);
        Guard.Argument(title, nameof(title))
            .NotNull();

        try
        {
            var response = await _notionService.UpdatePage(pageId, title);

            return $"Page updated successfully!\nID: {response.Id}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while updating Notion page: {Message}", ex.Message);
            return $"Exception occurred while updating Notion page: {ex.Message}";
        }
    }

    [McpServerTool(Name = "nb_update_page_content"),
    Description("Updates the notion page content.")]
    public async Task<string> UpdatePageContent(string pageId, string content)
    {
        _logger.LogInformation("Updating content for Notion page with ID: {PageId}", pageId);
        Guard.Argument(pageId, nameof(pageId))
            .NotNull()
            .NotEmpty();
        Guard.Argument(content, nameof(content))
            .NotNull();
    
        try
        {
            // Get existing blocks
            var blockChildrenResponse = await _notionService.RetrieveBlockChildren(pageId);
            if (blockChildrenResponse.Results.Any())
            {
                
                var blockIds = blockChildrenResponse.Results.Select(x => x.Id).ToList();
                await _notionService.DeleteBlocks(blockIds);
            }
        
            await _notionService.AppendBlockChildren(pageId, content);
    
            _logger.LogInformation("Successfully updated content for page ID: {PageId}", pageId);
            return $"Page content updated successfully!\nID: {pageId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while updating Notion page content: {Message}", ex.Message);
            return $"Exception occurred while updating Notion page content: {ex.Message}";
        }
    }
}