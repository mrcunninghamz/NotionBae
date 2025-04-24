using System.ComponentModel;
using System.Text.Json;
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

    public BlockTool(
        INotionService notionService,
        ILogger<BlockTool> logger)
    {
        _notionService = notionService;
        _logger = logger;
    }

    [McpServerTool(Name = "nb_retrieve_block_children"), 
     Description("Retrieves children blocks of a specified block or page ID and converts them to markdown. In otherwords it gets the content of a Notion page or block and converts it to markdown.")]
    public async Task<string> RetrieveBlockChildren(string blockId)
    {
        _logger.LogInformation("Retrieving block children for block/page ID: {BlockId}", blockId);
        
        try
        {
            var response = await _notionService.RetrieveBlockChildren(blockId);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var detailedError = NotionResponseHelper.ExtractErrorMessage(errorContent);
                
                _logger.LogError("Error retrieving block children: {StatusCode} with message: {Message}", 
                    response.StatusCode, detailedError);
                return $"Error retrieving block children: {response.StatusCode}\nDetails: {detailedError}";
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var blockResult = JsonDocument.Parse(responseContent);
            
            if (blockResult.RootElement.TryGetProperty("results", out var results))
            {
                var markdown = NotionToMarkdownConverter.ConvertToMarkdown(results);
                _logger.LogInformation("Successfully converted block children to markdown for ID: {BlockId}", blockId);
                return markdown;
            }
            
            _logger.LogWarning("No results found in the response for block ID: {BlockId}", blockId);
            return "No content found";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving block children: {Message}", ex.Message);
            return $"Exception occurred while retrieving block children: {ex.Message}";
        }
    }
}