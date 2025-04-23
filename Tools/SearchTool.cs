using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NotionBae.Services;

namespace NotionBae.Tools;

[McpServerToolType]
public class SearchTool
{
    private readonly INotionService _notionService;

    public SearchTool(INotionService notionService)
    {
        _notionService = notionService;
    }

    [McpServerTool, Description("Searches Notion pages and returns their titles and public URLs.")]
    public async Task<string> Search(string query)
    {
        var response = await _notionService.Search(query);
        
        if (!response.IsSuccessStatusCode)
        {
            return $"Error searching Notion: {response.StatusCode}";
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var searchResult = JsonDocument.Parse(responseContent);
        
        var results = new List<string>();
        
        if (searchResult.RootElement.TryGetProperty("results", out var resultsElement) && 
            resultsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var page in resultsElement.EnumerateArray())
            {
                string title = "";
                string publicUrl = "No public URL available";
                
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
                
                results.Add($"Title: {title}, Public URL: {publicUrl}");
            }
        }
        
        return results.Count > 0 
            ? string.Join("\n", results) 
            : "No results found";
    }
}
