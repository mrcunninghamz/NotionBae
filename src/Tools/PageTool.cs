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
     Description("Retrieves a Notion page with its metadata and full content in markdown format.")]
    public async Task<string> GetPageContent(
        string pageId)
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
            var markdownPageContent = new StringBuilder();
            await GetPageMDContent(pageId, markdownPageContent);

            // Combine metadata and content into a single response
            var markdownResponse = $"""
                            ---
                            - pageid: {pageId}
                            - privateUrl: {privateUrl}
                            - publicUrl: {publicUrl}
                            ---
                            {markdownPageContent}
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

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var detailedError = NotionResponseHelper.ExtractErrorMessage(errorContent);

                _logger.LogError("Error updating Notion page: {StatusCode} with message: {Message}",
                    response.StatusCode, detailedError);
                return $"Error updating Notion page: {response.StatusCode}\nDetails: {detailedError}";
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var pageResult = JsonDocument.Parse(responseContent);

            // Extract page ID to confirm success
            var updatedPageId = "";
            if (pageResult.RootElement.TryGetProperty("id", out var idProp))
            {
                updatedPageId = idProp.GetString() ?? "";
            }

            var updateInfo = new List<string>();
            if (!string.IsNullOrEmpty(title))
            {
                updateInfo.Add("title");
            }

            var updateDetails = string.Join(" and ", updateInfo);
            _logger.LogInformation("Successfully updated {UpdateDetails} for page ID: {PageId}", updateDetails, pageId);

            return $"Page updated successfully!\nID: {updatedPageId}\nUpdated: {updateDetails}";
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
            if (blockChildrenResponse.IsSuccessStatusCode)
            {
                var blockContent = await blockChildrenResponse.Content.ReadAsStringAsync();
                var blockResult = JsonDocument.Parse(blockContent);
                
                var blockIds = new List<string>();
                if (blockResult.RootElement.TryGetProperty("results", out var results))
                {
                    foreach (var block in results.EnumerateArray())
                    {
                        if (block.TryGetProperty("id", out var blockId))
                        {
                            blockIds.Add(blockId.GetString()!);
                        }
                    }
                }

                await _notionService.DeleteBlocks(blockIds);
            }
        
            var response = await _notionService.AppendBlockChildren(pageId, content);
    
            _logger.LogInformation("Successfully updated content for page ID: {PageId}", pageId);
            return $"Page content updated successfully!\nID: {pageId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while updating Notion page content: {Message}", ex.Message);
            return $"Exception occurred while updating Notion page content: {ex.Message}";
        }
    }

    private async Task GetPageMDContent(string blockId, StringBuilder markdownPageContent, int level = 0)
    {
        var contentResponse = await _notionService.RetrieveBlockChildren(blockId);

        if (!contentResponse.IsSuccessStatusCode)
        {
            var errorContent = await contentResponse.Content.ReadAsStringAsync();
            var detailedError = NotionResponseHelper.ExtractErrorMessage(errorContent);

            _logger.LogError("Error retrieving Notion page content: {StatusCode} with message: {Message}",
                contentResponse.StatusCode, detailedError);
            throw new Exception($"Error retrieving Notion page content: {contentResponse.StatusCode}\nDetails: {detailedError}");
        }

        var contentResponseContent = await contentResponse.Content.ReadAsStringAsync();
            
        var blockResult = JsonDocument.Parse(contentResponseContent);
            
        if (blockResult.RootElement.TryGetProperty("results", out var results))
        {
            foreach (var block in results.EnumerateArray())
            {
                var markdown = NotionToMarkdownConverter.ConvertToMarkdown(block, level);
                markdownPageContent.AppendLine(markdown);
                
                var hasChildren = false;
                if (block.TryGetProperty("has_children", out var hasChildrenProp))
                {
                    hasChildren = hasChildrenProp.GetBoolean();
                }

                if (!hasChildren)
                {
                    continue;
                }

                var parentBlockId = block.GetProperty("id").GetString()!;
                await GetPageMDContent(parentBlockId, markdownPageContent, level + 1);
            }
        }
    }
}