using System.Text;
using System.Text.Json;

namespace NotionBae.Utilities;

/// <summary>
/// Utility class to convert Notion API blocks into Markdown text
/// </summary>
public class NotionToMarkdownConverter
{
    /// <summary>
    /// Converts Notion block to markdown text
    /// </summary>
    /// <param name="block">a notion block</param>
    /// <returns>A string containing the markdown representation</returns>
    public static string ConvertToMarkdown(JsonElement block, int level = 0)
    {
        var markdown = new StringBuilder();
        var blockType = block.GetProperty("type").GetString();

        var tabs = GetTabs(level);
        
        // we need to create a comment that will display only for the Agent the id of the block
        markdown.AppendLine($"{tabs}[//]: # (BlockId: {block.GetProperty("id").GetString()})");
        switch (blockType)
        {
            case "heading_1":
                markdown.AppendLine($"# {ConvertRichTextToMarkdown(block.GetProperty("heading_1").GetProperty("rich_text"))}");
                break;
                
            case "heading_2":
                markdown.AppendLine($"## {ConvertRichTextToMarkdown(block.GetProperty("heading_2").GetProperty("rich_text"))}");
                break;
                
            case "heading_3":
                markdown.AppendLine($"### {ConvertRichTextToMarkdown(block.GetProperty("heading_3").GetProperty("rich_text"))}");
                break;
                
            case "paragraph":
                var paragraphText = ConvertRichTextToMarkdown(block.GetProperty("paragraph").GetProperty("rich_text"));
                if (!string.IsNullOrEmpty(paragraphText))
                {
                    markdown.AppendLine(tabs + paragraphText);
                }
                break;
                
            case "code":
                var codeBlock = block.GetProperty("code");
                var language = codeBlock.TryGetProperty("language", out var langElement) ? langElement.GetString() : "";
                markdown.AppendLine($"{tabs}```{language}");
                markdown.AppendLine(tabs + ConvertRichTextToMarkdown(codeBlock.GetProperty("rich_text"), level));
                markdown.AppendLine($"{tabs}```");
                break;
                
            case "bulleted_list_item":
                markdown.AppendLine($"{tabs}- {ConvertRichTextToMarkdown(block.GetProperty("bulleted_list_item").GetProperty("rich_text"))}");
                break;
                
            case "numbered_list_item":
                markdown.AppendLine($"{tabs}1. {ConvertRichTextToMarkdown(block.GetProperty("numbered_list_item").GetProperty("rich_text"))}");
                break;
        }
        
        markdown.AppendLine(); // Add empty line between blocks for better readability

        return markdown.ToString().TrimEnd();
    }

    /// <summary>
    /// Converts Notion rich text array to markdown formatted text
    /// </summary>
    private static string ConvertRichTextToMarkdown(JsonElement richTextArray, int level = 0)
    {
        var markdown = new StringBuilder();

        foreach (var textBlock in richTextArray.EnumerateArray())
        {
            var tabs = GetTabs(level);
            var content = textBlock.GetProperty("plain_text").GetString();
            var annotations = textBlock.GetProperty("annotations");
            
            // Apply tabs
            content = content.Replace("\n", $"\n{tabs}");

            // Apply formatting based on annotations
            if (annotations.GetProperty("code").GetBoolean())
            {
                content = $"`{content}`";
            }
            if (annotations.GetProperty("bold").GetBoolean() && annotations.GetProperty("italic").GetBoolean())
            {
                content = $"***{content}***";
            }
            else
            {
                if (annotations.GetProperty("bold").GetBoolean())
                {
                    content = $"**{content}**";
                }
                if (annotations.GetProperty("italic").GetBoolean())
                {
                    content = $"*{content}*";
                }
            }
            if (annotations.GetProperty("strikethrough").GetBoolean())
            {
                content = $"~~{content}~~";
            }

            markdown.Append(content);
        }

        return markdown.ToString();
    }

    private static string GetTabs(int level)
    {
        return new string('\t', level);
    }
}