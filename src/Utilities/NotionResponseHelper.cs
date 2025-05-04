using System.Text.Json;

namespace NotionBae.Utilities;

public static class NotionResponseHelper
{
    /// <summary>
    /// Extracts detailed error message from Notion API error response
    /// </summary>
    /// <param name="errorContent">The error response content as string</param>
    /// <returns>Extracted error message or default message if extraction fails</returns>
    public static string ExtractErrorMessage(string errorContent)
    {
        string detailedError = "Unknown error";
        
        try
        {
            var errorJson = JsonDocument.Parse(errorContent);
            if (errorJson.RootElement.TryGetProperty("message", out var errorMessage))
            {
                detailedError = errorMessage.GetString() ?? "Unknown error";
            }
        }
        catch (JsonException)
        {
            detailedError = errorContent;
        }
        
        return detailedError;
    }
}
