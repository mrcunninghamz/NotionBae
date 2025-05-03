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
                            ((dynamic) bulletBlock).bulleted_list_item.children = new List<object>()));
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
                                ((dynamic) numberBlock).numbered_list_item.children = new List<object>()));
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
    private static object CreateHeadingBlock(string content, string level = "heading_3")
    {
        var result = new Dictionary<string, object>
        {
            {"object", "block"},
            {"type", level}
        };

        var headingContent = new Dictionary<string, object>
        {
            {
                "rich_text",
                RichTextConverter.ConvertToRichText(content)
            }
        };

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
                rich_text = RichTextConverter.ConvertToRichText(content)
            }
        };
    }

    /// <summary>
    /// Creates a code block with validation for supported languages
    /// </summary>
    private static object CreateCodeBlock(string content, string language)
    {
        string normalizedLanguage = language.ToLowerInvariant().Trim();

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
                rich_text = RichTextConverter.ConvertToRichText(content),
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
                rich_text = RichTextConverter.ConvertToRichText(content)
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
                rich_text = RichTextConverter.ConvertToRichText(content)
            }
        };
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