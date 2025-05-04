using System.ComponentModel;
using System.Text.Json;
using Dawn;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using NotionBae.Services;
using NotionBae.Utilities;

namespace NotionBae.Tools;

[McpServerToolType]
public class BlockTool
{
    private readonly INotionService _notionService;
    private readonly ILogger<BlockTool> _logger;

    public BlockTool(INotionService notionService, ILogger<BlockTool> logger)
    {
        _notionService = notionService;
        _logger = logger;
    }

    [McpServerTool(Name = "nb_update_block"), Description("Updates a Notion block with markdown content.")]
    public async Task<string> UpdateBlock(
        string blockId,
        string content)
    {
        _logger.LogInformation("Updating Notion block with ID: {BlockId}", blockId);
        Guard.Argument(blockId, nameof(blockId))
            .NotNull()
            .NotEmpty();
        Guard.Argument(content, nameof(content))
            .NotNull();

        try
        {
            var response = await _notionService.UpdateBlock(blockId, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var detailedError = NotionResponseHelper.ExtractErrorMessage(errorContent);

                _logger.LogError("Error updating Notion block: {StatusCode} with message: {Message}",
                    response.StatusCode, detailedError);
                return $"Error updating Notion block: {response.StatusCode}\nDetails: {detailedError}";
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Successfully updated content for block ID: {BlockId}", blockId);

            return $"Block updated successfully!\nID: {blockId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while updating Notion block: {Message}", ex.Message);
            return $"Exception occurred while updating Notion block: {ex.Message}";
        }
    }

    [McpServerTool(Name = "nb_delete_blocks"), Description("Deletes a Notion block with the specified Ids. " +
                                                           "Before using this tool, make sure you have re-ran nb_get_page_content. " +
                                                           "When deleting sections send a list of blockIds for better efficiency.")]
    public async Task<string> DeleteBlocks(List<string> blockIds)
    {
        _logger.LogInformation("Deleting Notion block with ID: {BlockId}", blockIds);
        Guard.Argument(blockIds, nameof(blockIds))
            .NotNull()
            .NotEmpty();

        try
        {
            foreach(var id in blockIds){
                var response = await _notionService.DeleteBlock(id);

                if (response.IsSuccessStatusCode) continue;
                var errorContent = await response.Content.ReadAsStringAsync();
                var detailedError = NotionResponseHelper.ExtractErrorMessage(errorContent);

                _logger.LogError("Error deleting Notion block: {StatusCode} with message: {Message}",
                    response.StatusCode, detailedError);
                return $"Error deleting Notion block: {response.StatusCode}\nDetails: {detailedError}";
            }

            _logger.LogInformation("Successfully deleted block with ID: {BlockId}", blockIds);
            return $"Block deleted successfully!\nID: {blockIds}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while deleting Notion block: {Message}", ex.Message);
            return $"Exception occurred while deleting Notion block: {ex.Message}";
        }
    }
    
    [McpServerTool(Name = "nb_append_block_content"),
    Description("Appends a new block to an existing Notion page using markdown format. " +
                "If given a specific block id, you can append the new block after the specified block.")]
    public async Task<string> AppendBlockContent(
        string blockId, 
        string content, 
        [Description("The parent blockId of a specific block to append after.")]
        string parentBlockId = "")
    {
        _logger.LogInformation("Appending content to Notion block with ID: {BlockId}", blockId);
        Guard.Argument(blockId, nameof(blockId))
            .NotNull()
            .NotEmpty();
        Guard.Argument(content, nameof(content))
            .NotNull();
    
        try
        {
            var response = await _notionService.AppendBlockChildren(blockId, content, parentBlockId);
    
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var detailedError = NotionResponseHelper.ExtractErrorMessage(errorContent);
    
                _logger.LogError("Error appending Notion block content: {StatusCode} with message: {Message}",
                    response.StatusCode, detailedError);
                return $"Error appending Notion block content: {response.StatusCode}\nDetails: {detailedError}";
            }
    
            _logger.LogInformation("Successfully appended content to block ID: {BlockId}", blockId);
            return $"Block content appended successfully!\nID: {blockId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while appending Notion block content: {Message}", ex.Message);
            return $"Exception occurred while appending Notion block content: {ex.Message}";
        }
    }
}
