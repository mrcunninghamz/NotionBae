using System.Text;
using System.Text.Json;

namespace NotionBae.Utilities;

/// <summary>
/// Utility class to convert Notion API blocks into Markdown text
/// </summary>
public class NotionToMarkdownConverter
{
    /// <summary>
    /// Converts Notion blocks to markdown text
    /// </summary>
    /// <param name="blocks">List of Notion blocks to convert</param>
    /// <returns>A string containing the markdown representation</returns>
    public static string ConvertToMarkdown(JsonElement blocks)
    {
        var markdown = new StringBuilder();

        foreach (var block in blocks.EnumerateArray())
        {
            string blockType = block.GetProperty("type").GetString();
            
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
                        markdown.AppendLine(paragraphText);
                    }
                    break;
                    
                case "code":
                    var codeBlock = block.GetProperty("code");
                    var language = codeBlock.TryGetProperty("language", out var langElement) ? langElement.GetString() : "";
                    markdown.AppendLine($"```{language}");
                    markdown.AppendLine(ConvertRichTextToMarkdown(codeBlock.GetProperty("rich_text")));
                    markdown.AppendLine("```");
                    break;
                    
                case "bulleted_list_item":
                    markdown.AppendLine($"- {ConvertRichTextToMarkdown(block.GetProperty("bulleted_list_item").GetProperty("rich_text"))}");
                    break;
                    
                case "numbered_list_item":
                    markdown.AppendLine($"1. {ConvertRichTextToMarkdown(block.GetProperty("numbered_list_item").GetProperty("rich_text"))}");
                    break;
            }
            
            markdown.AppendLine(); // Add empty line between blocks for better readability
        }

        return markdown.ToString().TrimEnd();
    }

    /// <summary>
    /// Converts Notion rich text array to markdown formatted text
    /// </summary>
    private static string ConvertRichTextToMarkdown(JsonElement richTextArray)
    {
        var markdown = new StringBuilder();

        foreach (var textBlock in richTextArray.EnumerateArray())
        {
            string content = textBlock.GetProperty("plain_text").GetString();
            var annotations = textBlock.GetProperty("annotations");

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
}