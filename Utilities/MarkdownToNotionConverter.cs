using System.Text.RegularExpressions;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace NotionBae.Utilities;

/// <summary>
/// Utility class to convert Markdown text into Notion API block format
/// </summary>
public class MarkdownToNotionConverter
{
    // List of supported languages by Notion API
    private static readonly HashSet<string> SupportedLanguages = new HashSet<string>
    {
        "abap", "agda", "arduino", "ascii art", "assembly", "bash", "basic", "bnf", "c", "c#", "c++",
        "clojure", "coffeescript", "coq", "css", "dart", "dhall", "diff", "docker", "ebnf", "elixir", "elm",
        "erlang", "f#", "flow", "fortran", "gherkin", "glsl", "go", "graphql", "groovy", "haskell", "hcl",
        "html", "idris", "java", "javascript", "json", "julia", "kotlin", "latex", "less", "lisp", "livescript",
        "llvm ir", "lua", "makefile", "markdown", "markup", "matlab", "mathematica", "mermaid", "nix",
        "notion formula", "objective-c", "ocaml", "pascal", "perl", "php", "plain text", "powershell",
        "prolog", "protobuf", "purescript", "python", "r", "racket", "reason", "ruby", "rust", "sass",
        "scala", "scheme", "scss", "shell", "smalltalk", "solidity", "sql", "swift", "toml", "typescript",
        "vb.net", "verilog", "vhdl", "visual basic", "webassembly", "xml", "yaml", "java/c/c++/c#", "notionscript"
    };

    // Default language to use when the specified language is not supported
    private const string DefaultLanguage = "java/c/c++/c#";

    /// <summary>
    /// Converts markdown text to an array of Notion blocks
    /// </summary>
    /// <param name="markdown">The markdown text to convert</param>
    /// <returns>A list of objects representing Notion blocks</returns>
    public static List<object> ConvertToNotionBlocks(string markdown)
    {
        var blocks = new List<object>();
        
        // Split the markdown into lines for processing
        var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            
            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
                continue;
                
            // Check for headings
            if (line.StartsWith("### "))
            {
                blocks.Add(CreateHeadingBlock(line.Substring(4), "heading_3"));
            }
            else if (line.StartsWith("## "))
            {
                blocks.Add(CreateHeadingBlock(line.Substring(3), "heading_2"));
            }
            else if (line.StartsWith("# "))
            {
                blocks.Add(CreateHeadingBlock(line.Substring(2), "heading_1"));
            }
            // Check for code blocks
            else if (line.StartsWith("```"))
            {
                var codeContent = new StringBuilder();
                var language = line.Length > 3 ? line.Substring(3).Trim() : "";
                
                i++; // Skip the opening ```
                
                // Collect all code lines until closing ```
                while (i < lines.Length && !lines[i].StartsWith("```"))
                {
                    codeContent.AppendLine(lines[i]);
                    i++;
                }
                
                blocks.Add(CreateCodeBlock(codeContent.ToString(), language));
            }
            // Check for bullet lists
            else if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                blocks.Add(CreateBulletedListBlock(line.Substring(2)));
            }
            // Check for numbered lists
            else if (Regex.IsMatch(line, @"^\d+\.\s"))
            {
                var match = Regex.Match(line, @"^\d+\.\s(.+)$");
                if (match.Success)
                {
                    blocks.Add(CreateNumberedListBlock(match.Groups[1].Value));
                }
            }
            // Check for XML blocks
            else if (line.Contains("```xml"))
            {
                var xmlContent = new StringBuilder();
                
                i++; // Skip the opening ```xml
                
                // Collect all xml lines until closing ```
                while (i < lines.Length && !lines[i].StartsWith("```"))
                {
                    xmlContent.AppendLine(lines[i]);
                    i++;
                }
                
                blocks.Add(CreateCodeBlock(xmlContent.ToString(), "xml"));
            }
            // Regular paragraph
            else
            {
                blocks.Add(CreateParagraphBlock(line));
            }
        }
        
        return blocks;
    }
    
    /// <summary>
    /// Creates a heading block
    /// </summary>
    private static object CreateHeadingBlock(string content, string level = "heading_3")
    {
        // Create a dynamic object with the correct property name
        var result = new Dictionary<string, object>
        {
            { "object", "block" },
            { "type", level }
        };

        // Create the nested property with the same name as the level
        var headingContent = new Dictionary<string, object>
        {
            { 
                "rich_text", new[]
                {
                    new Dictionary<string, object>
                    {
                        { "type", "text" },
                        { 
                            "text", new Dictionary<string, string>
                            {
                                { "content", content }
                            }
                        }
                    }
                }
            }
        };

        // Add the heading content with the dynamic property name matching the level
        result.Add(level, headingContent);

        return result;
    }
    
    /// <summary>
    /// Creates a paragraph block
    /// </summary>
    private static object CreateParagraphBlock(string content)
    {
        return new
        {
            @object = "block",
            type = "paragraph",
            paragraph = new
            {
                rich_text = new[]
                {
                    new
                    {
                        type = "text",
                        text = new
                        {
                            content
                        }
                    }
                }
            }
        };
    }
    
    /// <summary>
    /// Creates a code block with validation for supported languages
    /// </summary>
    private static object CreateCodeBlock(string content, string language)
    {
        // Validate and normalize language
        string normalizedLanguage = language.ToLowerInvariant().Trim();
        
        // Use default language if the specified language is not supported
        if (!SupportedLanguages.Contains(normalizedLanguage))
        {
            normalizedLanguage = DefaultLanguage;
        }
        
        return new
        {
            @object = "block",
            type = "code",
            code = new
            {
                rich_text = new[]
                {
                    new
                    {
                        type = "text",
                        text = new
                        {
                            content
                        }
                    }
                },
                language = normalizedLanguage
            }
        };
    }
    
    /// <summary>
    /// Creates a bulleted list item block
    /// </summary>
    private static object CreateBulletedListBlock(string content)
    {
        return new
        {
            @object = "block",
            type = "bulleted_list_item",
            bulleted_list_item = new
            {
                rich_text = new[]
                {
                    new
                    {
                        type = "text",
                        text = new
                        {
                            content
                        }
                    }
                }
            }
        };
    }
    
    /// <summary>
    /// Creates a numbered list item block
    /// </summary>
    private static object CreateNumberedListBlock(string content)
    {
        return new
        {
            @object = "block",
            type = "numbered_list_item",
            numbered_list_item = new
            {
                rich_text = new[]
                {
                    new
                    {
                        type = "text",
                        text = new
                        {
                            content
                        }
                    }
                }
            }
        };
    }
}
