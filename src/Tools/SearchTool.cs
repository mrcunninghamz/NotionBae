using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Notion.Client;
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
            
            var results = new List<string>();
            
            foreach (var page in response.Results.Select(x => x as Page))
            {
                results.Add($"Title: {string.Join(' ', (page.Properties["title"] as TitlePropertyValue).Title.Select(x => x.PlainText))}, ID: {page.Id}, Public URL: {page.PublicUrl ?? "Not available"}");
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
