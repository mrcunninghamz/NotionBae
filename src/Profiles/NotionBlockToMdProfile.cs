using System.Xml.Serialization;
using AutoMapper;
using Markdig.Extensions.Tables;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Notion.Client;
using NotionBae.Utilities;
using Block = Notion.Client.Block;
using CodeBlock = Notion.Client.CodeBlock;
using HeadingBlock = Markdig.Syntax.HeadingBlock;
using IBlock = Notion.Client.IBlock;
using ParagraphBlock = Notion.Client.ParagraphBlock;

namespace NotionBae.Profiles;

public class NotionBlockToMdProfile : Profile
{
    public NotionBlockToMdProfile()
    {
        // Map from Notion Blocks to MarkdownDocument
        CreateMap<List<IBlock>, MarkdownDocument>()
            .ConvertUsing((blocks, _, context) =>
            {
                var document = new MarkdownDocument();
                context.Items["AllBlocks"] = document;
                foreach (var block in blocks)
                {
                    var markdownBlock = context.Mapper.Map<Markdig.Syntax.Block>(block);
                    if (markdownBlock != null)
                    {
                        document.Add(AddBlockIdComment(block.Id)); //come back to this.
                        document.Add(markdownBlock);
                    }
                }
                return document;
            });

        // Paragraph block mapping
        CreateMap<ParagraphBlock, MarkdownObject>()
            .ConvertUsing((src, _, context) =>
            {
                var items = context.Items["AllBlocks"] as MarkdownDocument;
                var hasChildren = false;
                var paragraphBlock = new Markdig.Syntax.ParagraphBlock();
                
                // Add rich text content to paragraph
                if (src.Paragraph?.RichText != null)
                {
                    var inlineContainer = new ContainerInline();
                    paragraphBlock.Inline = inlineContainer;
                    
                    foreach (var richText in src.Paragraph.RichText)
                    {
                        var inline = MapRichTextToInline(richText, context);
                        if (inline != null)
                        {
                            inlineContainer.AppendChild(inline);
                        }
                    }
                }
                
                // Map child blocks recursively
                hasChildren = src.Paragraph?.Children != null;
                if (hasChildren)
                {
                    items!.Add(paragraphBlock);
                    foreach (var child in src.Paragraph.Children)
                    {
                        var childMarkdownBlock = context.Mapper.Map<Markdig.Syntax.Block>(child);

                        if (childMarkdownBlock == null)
                        {
                            continue;
                        }
                        items.Add(AddBlockIdComment(src.Id));
                        items!.Add(childMarkdownBlock);
                    }
                }

                if (!hasChildren) return paragraphBlock;
                
                context.Items["AllBlocks"] = items;
                return null;
            });
        
        // Heading block mappings
        CreateMap<HeadingOneBlock, MarkdownObject>()
            .ConvertUsing((src, _, context) => CreateHeadingBlock(src.Heading_1?.RichText, 1, context));
        
        CreateMap<HeadingTwoBlock, MarkdownObject>()
            .ConvertUsing((src, _, context) => CreateHeadingBlock(src.Heading_2?.RichText, 2, context));
        
        CreateMap<HeadingThreeBlock, MarkdownObject>()
            .ConvertUsing((src, _, context) => CreateHeadingBlock(src.Heading_3?.RichText, 3, context));
        
        // Bulleted list mapping
        CreateMap<BulletedListBlock, MarkdownObject>()
            .ConvertUsing((src, _, context) => 
                new ListBlock(new ListBlockParser())
                {
                    IsOrdered = false,
                    BulletType = '-'
                });
        CreateMap<BulletedListItemBlock, MarkdownObject>()
            .ConvertUsing((src, _, context) =>
            {
                var items = context.Items["AllBlocks"] as MarkdownDocument;
                var parent = context.Items["Parent"] as ListItemBlock;
                var listBlock = parent == null ? items!.LastOrDefault() as ListBlock : parent.LastOrDefault(x => x is ContainerBlock) as ContainerBlock;
                
                if (listBlock == null)
                {
                    listBlock = new ListBlock(new ListBlockParser())
                    {
                        IsOrdered = false,
                        BulletType = '-'
                    };
                    
                    if(parent != null)
                    {
                        parent.Add(listBlock);
                    }
                    else
                    {
                        items!.Add(listBlock);
                    }
                }

                var listItem = new ListItemBlock(new ListBlockParser())
                {
                    Order = 0,
                    NewLine = NewLine.None
                };
                listBlock.Add(listItem);
                
                var paragraph = new Markdig.Syntax.ParagraphBlock();
                listItem.Add(AddBlockIdComment(src.Id));
                listItem.Add(paragraph);
                
                if (src.BulletedListItem?.RichText != null)
                {
                    var inlineContainer = new ContainerInline();
                    paragraph.Inline = inlineContainer;
                    
                    foreach (var richText in src.BulletedListItem.RichText)
                    {
                        var inline = MapRichTextToInline(richText, context);
                        if (inline != null)
                        {
                            inlineContainer.AppendChild(inline);
                        }
                    }
                }
                
                // Handle children (nested list items)
                var hasChildren = src.BulletedListItem?.Children != null;
                if (hasChildren)
                {
                    foreach (var child in src.BulletedListItem.Children)
                    {
                        // In a real scenario, this would need to handle nesting properly
                        context.Items["Parent"] = listBlock.Last() as ContainerBlock; 
                        var childMarkdownBlock = context.Mapper.Map<Markdig.Syntax.Block>(child);
                        if (childMarkdownBlock == null)
                        {
                            continue;
                        }
                        
                        items.Add(AddBlockIdComment(child.Id));
                        items!.Add(childMarkdownBlock);
                    }
                }
                context.Items["Parent"] = null;
                context.Items["AllBlocks"] = items;
                return null;
            });
        
        // Numbered list mapping
        CreateMap<NumberedListBlock, MarkdownObject>()
            .ConvertUsing((src, _, context) => 
                new ListBlock(new ListBlockParser())
                {
                    IsOrdered = true,
                    OrderedStart = "1",
                    OrderedDelimiter = '.',
                    BulletType = '1'
                });
        CreateMap<NumberedListItemBlock, MarkdownObject>()
            .ConvertUsing((src, _, context) =>
            {
                var items = context.Items["AllBlocks"] as MarkdownDocument;
                var parent = context.Items["Parent"] as ListItemBlock;
                var listBlock = parent == null ? items!.LastOrDefault() as ListBlock : parent.FirstOrDefault(x => x is ContainerBlock) as ContainerBlock;
                
                if (listBlock == null)
                {
                    listBlock = new ListBlock(new ListBlockParser())
                    {
                        IsOrdered = true,
                        OrderedStart = "1",
                        OrderedDelimiter = '.',
                        BulletType = '1'
                    };
                    
                    if(parent != null)
                    {
                        parent.Add(listBlock);
                    }
                    else
                    {
                        items!.Add(listBlock);
                    }
                }

                var listItem = new ListItemBlock(new ListBlockParser())
                {
                    Order = 0,
                    NewLine = NewLine.None
                };
                listBlock.Add(listItem);
                
                var paragraph = new Markdig.Syntax.ParagraphBlock();
                listItem.Add(AddBlockIdComment(src.Id));
                listItem.Add(paragraph);
                
                if (src.NumberedListItem?.RichText != null)
                {
                    var inlineContainer = new ContainerInline();
                    paragraph.Inline = inlineContainer;
                    
                    foreach (var richText in src.NumberedListItem.RichText)
                    {
                        var inline = MapRichTextToInline(richText, context);
                        if (inline != null)
                        {
                            inlineContainer.AppendChild(inline);
                        }
                    }
                }
                
                // Handle children (nested list items)
                var hasChildren = src.NumberedListItem?.Children != null;
                if (hasChildren)
                {
                    foreach (var child in src.NumberedListItem.Children)
                    {
                        // In a real scenario, this would need to handle nesting properly
                        context.Items["Parent"] = listBlock.Last() as ContainerBlock; 
                        var childMarkdownBlock = context.Mapper.Map<Markdig.Syntax.Block>(child);
                        if (childMarkdownBlock == null)
                        {
                            continue;
                        }
                        
                        items.Add(AddBlockIdComment(child.Id));
                        items!.Add(childMarkdownBlock);
                    }
                }
                context.Items["Parent"] = null;
                context.Items["AllBlocks"] = items;
                return null;
            });
        
        CreateMap<ImageBlock, MarkdownObject>()
            .ConvertUsing((src, _, context) =>
            {
                var text = string.Empty;
                switch (src.Image)
                {
                    case UploadedFile uploadedFile:
                        text = $"![{uploadedFile.Caption}]({uploadedFile.File.Url} \"{uploadedFile.Name}\"))";
                        break;
                    case ExternalFile externalFile:
                        text = $"![{externalFile.Caption}]({externalFile.External.Url} \"{externalFile.Name}\"))";
                        break;
                }
                
                
                var imageBlock = new Markdig.Syntax.ParagraphBlock
                {
                    Inline = new ContainerInline()
                        .AppendChild(new LiteralInline(text))
                };
                
                return imageBlock;
            });
        
        // Quote block mapping
        CreateMap<Notion.Client.QuoteBlock, MarkdownObject>()
            .ConvertUsing((src, _, context) =>
            {
                var quoteBlock = new Markdig.Syntax.QuoteBlock(new QuoteBlockParser());
                
                var paragraph = new Markdig.Syntax.ParagraphBlock();
                quoteBlock.Add(paragraph);
                
                if (src.Quote?.RichText != null)
                {
                    var inlineContainer = new ContainerInline();
                    paragraph.Inline = inlineContainer;
                    
                    foreach (var richText in src.Quote.RichText)
                    {
                        var inline = MapRichTextToInline(richText, context);
                        if (inline != null)
                        {
                            inlineContainer.AppendChild(inline);
                        }
                    }
                }
                
                return quoteBlock;
            });
        
        // Code block mapping
        CreateMap<Notion.Client.CodeBlock, MarkdownObject>()
            .ConvertUsing((src, _, context) =>
            {
                var codeBlock = new Markdig.Syntax.FencedCodeBlock(new FencedCodeBlockParser())
                {
                    FencedChar = '`',
                    OpeningFencedCharCount = 3,
                    ClosingFencedCharCount = 3
                };
                
                // Set the language
                codeBlock.Info = MapNotionLanguageToMarkdown(src.Code?.Language);
                
                // Add the code content
                if (src.Code?.RichText != null)
                {
                    string codeContent = string.Join("", src.Code.RichText.Select(rt => 
                        rt is RichTextText textBlock ? textBlock.Text?.Content : ""));
                    
                    codeBlock.Lines = new Markdig.Helpers.StringLineGroup(codeContent);
                }
                
                return codeBlock;
            });
        
        // Divider/ThematicBreak mapping
        CreateMap<DividerBlock, MarkdownObject>()
            .ConvertUsing((_, _, _) => new ThematicBreakBlock(null));
        
        // Table mapping
        CreateMap<TableBlock, MarkdownObject>()
            .ConvertUsing((src, _, context) =>
            {
                var table = new Table(new GridTableParser());
                
                if (src.Table?.Children != null)
                {
                    bool isFirstRow = true;
                    foreach (var rowBlock in src.Table.Children)
                    {
                        if (rowBlock is TableRowBlock tableRowBlock)
                        {
                            var row = new TableRow();
                            
                            // Mark first row as header if HasColumnHeader is true
                            if (isFirstRow && src.Table.HasColumnHeader == true)
                            {
                                row.IsHeader = true;
                            }
                            
                            if (tableRowBlock.TableRow?.Cells != null)
                            {
                                foreach (var cellContent in tableRowBlock.TableRow.Cells)
                                {
                                    var cell = new TableCell(new GridTableParser());
                                    
                                    // Create a paragraph for the cell content
                                    var paragraph = new Markdig.Syntax.ParagraphBlock();
                                    var inlineContainer = new ContainerInline();
                                    paragraph.Inline = inlineContainer;
                                    
                                    foreach (var richText in cellContent)
                                    {
                                        var inline = MapRichTextToInline(richText, context);
                                        if (inline != null)
                                        {
                                            inlineContainer.AppendChild(inline);
                                        }
                                    }
                                    
                                    cell.Add(paragraph);
                                    row.Add(cell);
                                }
                            }
                            
                            table.Add(row);
                            isFirstRow = false;
                        }
                    }
                }
                
                // Set table column definitions
                if (src.Table?.TableWidth > 0)
                {
                    table.ColumnDefinitions.AddRange(new List<TableColumnDefinition>());
                    for (int i = 0; i <= src.Table.TableWidth; i++)
                    {
                        table.ColumnDefinitions.Add(new TableColumnDefinition());
                    }
                }
                
                return table;
            });
    }
    
    private HeadingBlock CreateHeadingBlock(IEnumerable<RichTextBase> richText, int level, ResolutionContext context)
    {
        var headingBlock = new HeadingBlock(null)
        {
            Level = level
        };
        
        if (richText != null)
        {
            var inlineContainer = new ContainerInline();
            headingBlock.Inline = inlineContainer;
            
            foreach (var rt in richText)
            {
                var inline = MapRichTextToInline(rt, context);
                if (inline != null)
                {
                    inlineContainer.AppendChild(inline);
                }
            }
        }
        
        return headingBlock;
    }
    
    private Markdig.Syntax.ParagraphBlock AddBlockIdComment(string blockId)
    {
        var comment = new LiteralInline($"[//]: # (BlockId: {blockId})");
        return 
            new Markdig.Syntax.ParagraphBlock
            {
                Inline = new ContainerInline()
                    .AppendChild(comment)
            };
        
    }
    
    private Inline MapRichTextToInline(RichTextBase richText, ResolutionContext context)
    {
        if (richText is RichTextText textBlock)
        {
            string content = textBlock.Text?.Content ?? string.Empty;
            
            // Handle link
            if (textBlock.Text?.Link != null)
            {
                return new LinkInline
                {
                    Url = textBlock.Text.Link.Url,
                    Title = content,
                };
            }
            
            // Handle text with annotations
            if (textBlock.Annotations != null)
            {
                if (!textBlock.Annotations.IsBold 
                    && !textBlock.Annotations.IsItalic 
                    && !textBlock.Annotations.IsStrikeThrough 
                    && !textBlock.Annotations.IsCode)
                {
                    return new LiteralInline(content);
                }
                    
                // Determine the emphasis character and count
                char delimiterChar;
                int delimiterCount;
                    
                if (textBlock.Annotations.IsItalic)
                {
                    delimiterChar = '*';
                    delimiterCount = 1;
                }
                else if (textBlock.Annotations.IsBold)
                {
                    delimiterChar = '*';
                    delimiterCount = 2;
                }
                else if (textBlock.Annotations.IsCode)
                {
                    delimiterChar = '`';
                    delimiterCount = 1;
                }
                else if (textBlock.Annotations.IsStrikeThrough)
                {
                    delimiterChar = '~';
                    delimiterCount = 2;
                }
                else
                {
                    delimiterChar = '*';
                    delimiterCount = 1;
                }
                
                // Apply formatting based on annotations
                if (textBlock.Annotations.IsCode)
                {
                    return new CodeInline(content)
                    {
                        Delimiter = delimiterChar,
                        DelimiterCount = delimiterCount
                    };
                }
                    
                var emphasis = new EmphasisInline
                {
                    DelimiterChar = delimiterChar,
                    DelimiterCount = delimiterCount
                };
                emphasis.AppendChild(new LiteralInline(content));
                    
                return emphasis;
            }
            
            // Default to literal text
            return new LiteralInline(content);
        }
        
        return null;
    }
    
    private string MapNotionLanguageToMarkdown(string notionLanguage)
    {
        // Perform the reverse mapping of MdToNotionBlockProfile.MapToNotionCodeLanguage
        return notionLanguage switch
        {
            "abap" => "abap",
            "agda" => "agda",
            "arduino" => "arduino",
            "ascii art" => "ascii",
            "assembly" => "asm",
            "bash" => "bash",
            "basic" => "basic",
            "bnf" => "bnf",
            "c" => "c",
            "c#" => "csharp",
            "c++" => "cpp",
            "clojure" => "clojure",
            "coffeescript" => "coffee",
            "coq" => "coq",
            "css" => "css",
            "dart" => "dart",
            "dhall" => "dhall",
            "diff" => "diff",
            "docker" => "dockerfile",
            "ebnf" => "ebnf",
            "elixir" => "elixir",
            "elm" => "elm",
            "erlang" => "erlang",
            "f#" => "fsharp",
            "flow" => "flow",
            "fortran" => "fortran",
            "gherkin" => "gherkin",
            "glsl" => "glsl",
            "go" => "go",
            "graphql" => "graphql",
            "groovy" => "groovy",
            "haskell" => "haskell",
            "hcl" => "hcl",
            "html" => "html",
            "idris" => "idris",
            "java" => "java",
            "javascript" => "javascript",
            "json" => "json",
            "julia" => "julia",
            "kotlin" => "kotlin",
            "latex" => "latex",
            "less" => "less",
            "lisp" => "lisp",
            "livescript" => "livescript",
            "llvm ir" => "llvm",
            "lua" => "lua",
            "makefile" => "makefile",
            "markdown" => "markdown",
            "markup" => "markup",
            "matlab" => "matlab",
            "mathematica" => "mathematica",
            "mermaid" => "mermaid",
            "nix" => "nix",
            "notion formula" => "notion-formula",
            "objective-c" => "objc",
            "ocaml" => "ocaml",
            "pascal" => "pascal",
            "perl" => "perl",
            "php" => "php",
            "plain text" => "text",
            "powershell" => "powershell",
            "prolog" => "prolog",
            "protobuf" => "proto",
            "purescript" => "purescript",
            "python" => "python",
            "r" => "r",
            "racket" => "racket",
            "reason" => "reason",
            "ruby" => "ruby",
            "rust" => "rust",
            "sass" => "sass",
            "scala" => "scala",
            "scheme" => "scheme",
            "scss" => "scss",
            "shell" => "shell",
            "smalltalk" => "smalltalk",
            "solidity" => "solidity",
            "sql" => "sql",
            "swift" => "swift",
            "toml" => "toml",
            "typescript" => "typescript",
            "vb.net" => "vb",
            "verilog" => "verilog",
            "vhdl" => "vhdl",
            "visual basic" => "visual basic",
            "webassembly" => "wasm",
            "xml" => "xml",
            "yaml" => "yaml",
            "java/c/c++/c#" => "java/c/c++/c#",
            "notionscript" => "notionscript",
            _ => string.Empty
        };
    }
}