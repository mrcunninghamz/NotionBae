using System.Text.RegularExpressions;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace NotionBae.Utilities;


public abstract class ContentWithChildren
{
    [JsonIgnore]
    public List<object>? Children { get; set; } = new();

    [JsonPropertyName("children")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<object>? SerializationChildren
    {
        get => Children?.Count > 0 ? Children : null;
        set => Children = value ?? new();
    }
}

/// <summary>
/// Base class for all Notion blocks
/// </summary>
public abstract class NotionBlock
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = "block";
    
    [JsonPropertyName("type")]
    public string Type { get; set; }
}
/// <summary>
/// Base class for heading blocks in Notion
/// </summary>
public abstract class HeadingBlock : NotionBlock
{
    protected HeadingBlock(string level)
    {
        Type = level;
    }
}

/// <summary>
/// Represents a level 1 heading block in Notion
/// </summary>
public class Heading1Block : HeadingBlock
{
    [JsonPropertyName("heading_1")]
    public HeadingContent Content { get; set; }

    public Heading1Block(object[] richText) : base("heading_1")
    {
        Content = new HeadingContent
        {
            RichText = richText
        };
    }
}

/// <summary>
/// Represents a level 2 heading block in Notion  
/// </summary>
public class Heading2Block : HeadingBlock
{
    [JsonPropertyName("heading_2")]
    public HeadingContent Content { get; set; }

    public Heading2Block(object[] richText) : base("heading_2")
    {
        Content = new HeadingContent
        {
            RichText = richText
        };
    }
}

/// <summary>
/// Represents a level 3 heading block in Notion
/// </summary>
public class Heading3Block : HeadingBlock
{
    [JsonPropertyName("heading_3")]
    public HeadingContent Content { get; set; }

    public Heading3Block(object[] richText) : base("heading_3")
    {
        Content = new HeadingContent
        {
            RichText = richText
        };
    }
}

public class HeadingContent
{
    [JsonPropertyName("rich_text")]
    public object[] RichText { get; set; }
}

/// <summary>
/// Represents a paragraph block in Notion
/// </summary>
public class ParagraphBlock : NotionBlock
{
    [JsonPropertyName("paragraph")]
    public ParagraphContent Paragraph { get; set; }

    public ParagraphBlock(object[] richText)
    {
        Type = "paragraph";
        Paragraph = new ParagraphContent
        {
            RichText = richText
        };
    }
}

public class ParagraphContent
{
    [JsonPropertyName("rich_text")]
    public object[] RichText { get; set; }
}

/// <summary>
/// Represents a code block in Notion
/// </summary>
public class CodeBlock : NotionBlock
{
    [JsonPropertyName("code")]
    public CodeContent Code { get; set; }

    public CodeBlock(object[] richText, string language)
    {
        Type = "code";
        Code = new CodeContent
        {
            RichText = richText,
            Language = language
        };
    }
}

public class CodeContent
{
    [JsonPropertyName("rich_text")]
    public object[] RichText { get; set; }
    
    [JsonPropertyName("language")]
    public string Language { get; set; }
}

/// <summary>
/// Represents a bulleted list item block in Notion
/// </summary>
public class BulletedListBlock : NotionBlock
{
    [JsonPropertyName("bulleted_list_item")]
    public BulletedListContent BulletedListItem { get; set; }

    public BulletedListBlock(object[] richText)
    {
        Type = "bulleted_list_item";
        BulletedListItem = new BulletedListContent
        {
            RichText = richText
        };
    }
}

public class BulletedListContent : ContentWithChildren
{
    [JsonPropertyName("rich_text")]
    public object[] RichText { get; set; }
}

/// <summary>
/// Represents a numbered list item block in Notion
/// </summary>
public class NumberedListBlock : NotionBlock
{
    [JsonPropertyName("numbered_list_item")]
    public NumberedListContent NumberedListItem { get; set; }

    public NumberedListBlock(object[] richText)
    {
        Type = "numbered_list_item";
        NumberedListItem = new NumberedListContent
        {
            RichText = richText
        };
    }
}

public class NumberedListContent : ContentWithChildren
{
    [JsonPropertyName("rich_text")]
    public object[] RichText { get; set; }
}

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
    private static int GetIndentationLevel(string line)
    {
        int spaces = 0;
        foreach (char c in line)
        {
            if (c == ' ')
                spaces++;
            else
                break;
        }

        return spaces / 2;
    }

    public static List<object> ConvertToNotionBlocks(string markdown)
    {
        var blocks = new List<object>();
        var blockStack = new Stack<(int Level, List<object> Children)>();
        blockStack.Push((0, blocks));

        // Split the markdown into lines for processing
        var lines = markdown.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var indentLevel = GetIndentationLevel(line);
            var trimmedLine = line.Trim();

            // Adjust stack based on indentation
            while (blockStack.Count > 1 && blockStack.Peek().Level >= indentLevel)
            {
                blockStack.Pop();
            }

            switch (trimmedLine)
            {
                // Headings
                case var _ when Regex.IsMatch(trimmedLine, @"^#{3,}\s"):
                {
                    blockStack.Peek().Children
                        .Add(CreateHeadingBlock(trimmedLine[(trimmedLine.IndexOf(' ') + 1)..], "heading_3"));
                    break;
                }
                case var _ when trimmedLine.StartsWith("## "):
                {
                    blockStack.Peek().Children.Add(CreateHeadingBlock(trimmedLine.Substring(3), "heading_2"));
                    break;
                }
                case var _ when trimmedLine.StartsWith("# "):
                {
                    blockStack.Peek().Children.Add(CreateHeadingBlock(trimmedLine.Substring(2), "heading_1"));
                    break;
                }
                // Code blocks
                case var _ when trimmedLine.StartsWith("```"):
                {
                    var codeContent = new StringBuilder();
                    var language = trimmedLine.Length > 3 ? trimmedLine.Substring(3).Trim() : "";

                    i++; // Skip the opening ```

                    while (i < lines.Length && !lines[i].Trim().StartsWith("```"))
                    {
                        codeContent.AppendLine(lines[i]);
                        i++;
                    }

                    blockStack.Peek().Children.Add(CreateCodeBlock(codeContent.ToString(), language));
                    break;
                }
                // Bullet lists    
                case var _ when trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* "):
                {
                    var bulletBlock = CreateBulletedListBlock(trimmedLine.Substring(2));
                    blockStack.Peek().Children.Add(bulletBlock);
                    if (i + 1 < lines.Length && GetIndentationLevel(lines[i + 1]) > indentLevel)
                    {
                        blockStack.Push((indentLevel + 1,
                            ((BulletedListBlock)bulletBlock).BulletedListItem.Children));
                    }

                    break;
                }
                // Numbered lists
                case var _ when Regex.IsMatch(trimmedLine, @"^\d+\.\s"):
                {
                    var match = Regex.Match(trimmedLine, @"^\d+\.\s(.+)$");
                    if (match.Success)
                    {
                        var numberBlock = CreateNumberedListBlock(match.Groups[1].Value);
                        blockStack.Peek().Children.Add(numberBlock);
                        if (i + 1 < lines.Length && GetIndentationLevel(lines[i + 1]) > indentLevel)
                        {
                            blockStack.Push((indentLevel + 1,
                                ((NumberedListBlock)numberBlock).NumberedListItem.Children));
                        }
                    }

                    break;
                }
                // Regular paragraph
                default:
                {
                    blockStack.Peek().Children.Add(CreateParagraphBlock(trimmedLine));
                    break;
                }
            }
        }

        return blocks;
    }

    /// <summary>
    /// Creates a heading block
    /// </summary>
    private static HeadingBlock CreateHeadingBlock(string content, string level = "heading_3")
    {
        var richText = RichTextConverter.ConvertToRichText(content);
        return level switch
        {
            "heading_1" => new Heading1Block(richText),
            "heading_2" => new Heading2Block(richText),
            _ => new Heading3Block(richText)
        };
    }

    /// <summary>
    /// Creates a paragraph block
    /// </summary>
    private static ParagraphBlock CreateParagraphBlock(string content)
    {
        var richText = RichTextConverter.ConvertToRichText(content);
        return new ParagraphBlock(richText);
    }

    /// <summary>
    /// Creates a code block with validation for supported languages
    /// </summary>
    private static CodeBlock CreateCodeBlock(string content, string language)
    {
        string normalizedLanguage = language.ToLowerInvariant().Trim();

        if (!SupportedLanguages.Contains(normalizedLanguage))
        {
            normalizedLanguage = DefaultLanguage;
        }

        var richText = RichTextConverter.ConvertToRichText(content);
        return new CodeBlock(richText, normalizedLanguage);
    }

    /// <summary>
    /// Creates a bulleted list item block
    /// </summary>
    private static BulletedListBlock CreateBulletedListBlock(string content)
    {
        var richText = RichTextConverter.ConvertToRichText(content);
        return new BulletedListBlock(richText);
    }

    /// <summary>
    /// Creates a numbered list item block
    /// </summary>
    private static NumberedListBlock CreateNumberedListBlock(string content)
    {
        var richText = RichTextConverter.ConvertToRichText(content);
        return new NumberedListBlock(richText);
    }
}

public class RichTextConverter
{
    public static object[] ConvertToRichText(string markdown)
    {
        var richTextBlocks = new List<object>();
        int currentPosition = 0;

        // Regex pattern to match different Markdown patterns
        var patterns = new[]
        {
            (@"(?<![*_])[*_](?![*_])(.+?)(?<![*_])[*_](?![*_])",
                new TextAnnotations {Italic = true}), // *italic* or _italic_
            (@"(?<![*_])[*_]{2}(?![*_])(.+?)(?<![*_])[*_]{2}(?![*_])",
                new TextAnnotations {Bold = true}), // **bold** or __bold__
            (@"(?<![*_])[*_]{3}(?![*_])(.+?)(?<![*_])[*_]{3}(?![*_])",
                new TextAnnotations {Bold = true, Italic = true}), // ***bold+italic*** or ___bold+italic___
            (@"~~(.+?)~~", new TextAnnotations {Strikethrough = true}), // ~~strikethrough~~
            (@"(?<!`)`(?!`)(.+?)(?<!`)`(?!`)", new TextAnnotations {Code = true}), // `code`
        };

        // Find all markdown patterns and their positions
        var segments = new List<(int Start, int End, string Text, TextAnnotations Annotations)>();

        // Add initial text if it exists
        foreach (var (pattern, annotations) in patterns)
        {
            var matches = Regex.Matches(markdown, pattern);
            foreach (Match match in matches)
            {
                segments.Add((match.Index, match.Index + match.Length,
                    match.Groups[1].Value, annotations));
            }
        }

        // Sort segments by start position
        segments = segments.OrderBy(s => s.Start).ToList();

        // Process text including non-formatted parts
        int lastEnd = 0;
        foreach (var segment in segments)
        {
            // Add plain text before the formatted segment
            if (segment.Start > lastEnd)
            {
                string plainText = markdown.Substring(lastEnd, segment.Start - lastEnd);
                if (!string.IsNullOrEmpty(plainText))
                {
                    richTextBlocks.Add(CreateRichTextBlock(plainText, new TextAnnotations()));
                }
            }

            // Add the formatted segment
            richTextBlocks.Add(CreateRichTextBlock(segment.Text, segment.Annotations));
            lastEnd = segment.Start + (segment.End - segment.Start);
        }

        // Add any remaining plain text
        if (lastEnd < markdown.Length)
        {
            string remainingText = markdown.Substring(lastEnd);
            if (!string.IsNullOrEmpty(remainingText))
            {
                richTextBlocks.Add(CreateRichTextBlock(remainingText, new TextAnnotations()));
            }
        }

        return richTextBlocks.ToArray();
    }

    private class TextAnnotations
    {
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Strikethrough { get; set; }
        public bool Underline { get; set; }
        public bool Code { get; set; }
        public string Color { get; set; } = "default";
    }

    private static object CreateRichTextBlock(string content, TextAnnotations annotations)
    {
        return new
        {
            type = "text",
            text = new
            {
                content = content,
                link = (string?) null
            },
            annotations = new
            {
                bold = annotations.Bold,
                italic = annotations.Italic,
                strikethrough = annotations.Strikethrough,
                underline = annotations.Underline,
                code = annotations.Code,
                color = annotations.Color
            },
            plain_text = content,
            href = (string?) null
        };
    }
}