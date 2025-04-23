using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using NotionBae.Services;
using NotionBae.Utilities;

namespace NotionBae.Tools;

[McpServerToolType]
public class SearchTool
{
    private readonly INotionService _notionService;
    private readonly ILogger<SearchTool> _logger;

    public SearchTool(INotionService notionService, ILogger<SearchTool> logger)
    {
        _notionService = notionService;
        _logger = logger;
    }

    [McpServerTool(Name = "nb_search"), Description("Searches Notion pages and returns their titles and public URLs.")]
    public async Task<string> Search(string query)
    {
        _logger.LogInformation("Searching Notion with query: {Query}", query);
        
        try
        {
            var response = await _notionService.Search(query);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var detailedError = NotionResponseHelper.ExtractErrorMessage(errorContent);
                
                _logger.LogError("Error searching Notion: {StatusCode} with message: {Message}", 
                    response.StatusCode, detailedError);
                return $"Error searching Notion: {response.StatusCode}\nDetails: {detailedError}";
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var searchResult = JsonDocument.Parse(responseContent);
            
            var results = new List<string>();
            
            if (searchResult.RootElement.TryGetProperty("results", out var resultsElement) && 
                resultsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var page in resultsElement.EnumerateArray())
                {
                    var title = "";
                    var publicUrl = "No public URL available";
                    var id = "";
                    var parentInfo = "";
                    
                    // Extract page ID
                    if (page.TryGetProperty("id", out var idProp))
                    {
                        id = idProp.GetString() ?? "";
                    }
                    
                    // Extract parent information
                    if (page.TryGetProperty("parent", out var parentProp))
                    {
                        if (parentProp.TryGetProperty("type", out var parentType))
                        {
                            var type = parentType.GetString();
                            parentInfo = $"Parent type: {type}";
                            
                            // Add additional parent info if available
                            if (type != null && parentProp.TryGetProperty(type, out var parentDetails))
                            {
                                if (parentDetails.ValueKind == JsonValueKind.String)
                                {
                                    parentInfo += $", ID: {parentDetails.GetString()}";
                                }
                                else if (parentDetails.ValueKind == JsonValueKind.True)
                                {
                                    parentInfo += ", workspace: true";
                                }
                            }
                        }
                    }
                    
                    // Extract title
                    if (page.TryGetProperty("properties", out var properties) &&
                        properties.TryGetProperty("title", out var titleProp) &&
                        titleProp.TryGetProperty("title", out var titleArray) &&
                        titleArray.ValueKind == JsonValueKind.Array && 
                        titleArray.GetArrayLength() > 0)
                    {
                        var firstTitle = titleArray[0];
                        if (firstTitle.TryGetProperty("text", out var text) &&
                            text.TryGetProperty("content", out var content))
                        {
                            title = content.GetString() ?? "";
                        }
                    }
                    
                    // Extract public URL
                    if (page.TryGetProperty("public_url", out var publicUrlProp) && 
                        publicUrlProp.ValueKind != JsonValueKind.Null)
                    {
                        publicUrl = publicUrlProp.GetString() ?? publicUrl;
                    }
                    
                    results.Add($"Title: {title}, ID: {id}, Parent: {parentInfo}, Public URL: {publicUrl}");
                }
            }
            
            _logger.LogInformation("Search completed. Found {ResultCount} results", results.Count);
            return results.Count > 0 
                ? string.Join("\n", results) 
                : "No results found";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while searching Notion: {Message}", ex.Message);
            return $"Exception occurred while searching Notion: {ex.Message}";
        }
    }
}
