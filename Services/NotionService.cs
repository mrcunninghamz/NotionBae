using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace NotionBae.Services;

public interface INotionService
{
    Task<HttpResponseMessage> Search(string query);
}

public class NotionService : INotionService
{
    private readonly HttpClient _httpclient;

    public NotionService(HttpClient httpclient, IConfiguration configuration)
    {
        var token = configuration["NotionApiKey"];
        _httpclient = httpclient;
        _httpclient.BaseAddress = new Uri("https://api.notion.com/v1/");
        _httpclient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");
        _httpclient.DefaultRequestHeaders.Remove("Authorization");
        _httpclient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }
    
    public async Task<HttpResponseMessage> Search(string query)
    {
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(new { query }),
            System.Text.Encoding.UTF8,
            "application/json");
            
        return await _httpclient.PostAsync("search", jsonContent);
    }
}