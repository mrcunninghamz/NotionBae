using AutoMapper;
using Markdig.Extensions.Footnotes;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Notion.Client;
using Block = Notion.Client.Block;
using CodeBlock = Markdig.Syntax.CodeBlock;
using IBlock = Notion.Client.IBlock;
using ParagraphBlock = Markdig.Syntax.ParagraphBlock;
using QuoteBlock = Markdig.Syntax.QuoteBlock;

namespace NotionBae.Profiles;

public class NotionBlockProfile : Profile
{
    public NotionBlockProfile()
    {
        // Map from Markdig MarkdownDocument to a list of Notion Blocks
        CreateMap<MarkdownDocument, List<IBlock>>()
            .ConvertUsing((doc, _, context) => 
            {
                var blocks = new List<IBlock>();
                context.Items["AllBlocks"] = blocks;
                foreach (var block in doc)
                {
                    var mappedBlock = context.Mapper.Map<Block>(block);
                    if (mappedBlock != null)
                    {
                        blocks.Add(mappedBlock);
                    }
                }
                return blocks;
            });
        
        CreateMap<LinkInline, RichTextBase>()
            .ConvertUsing((src, _, _) => new RichTextText
            {
                Text = new Text
                {
                    Content = string.IsNullOrEmpty(src.Title) ? src.Url : src.Title,
                    Link = new Link
                    {
                        Url = src.Url
                    }
                }
            });
        
        CreateMap<LiteralInline, RichTextBase>()
            .ConvertUsing((src, _, _) => new RichTextText
            {
                Text = new Text
                {
                    Content = src.ToString()
                },
                Annotations = new Annotations
                {
                    IsBold = false,
                    IsItalic = false,
                    IsStrikeThrough = false,
                    IsUnderline = false,
                    IsCode = false,
                    Color = Color.Default
                },
                PlainText = src.ToString()
            });
        
        CreateMap<EmphasisInline, RichTextBase>()
            .ConvertUsing((src, _, _) => new RichTextText
            {
                Text = new Text
                {
                    Content = src.FirstChild.ToString()
                },
                Annotations = new Annotations
                {
                    IsBold = src.DelimiterChar is '*' or '_' && src.DelimiterCount is 2 or 3,
                    IsItalic = src.DelimiterChar is '*' or '_' && src.DelimiterCount is 1 or 3,
                    IsStrikeThrough = src.DelimiterChar is '~' && src.DelimiterCount is 2,
                    IsUnderline = false,
                    IsCode = src.DelimiterChar is '`' && src.DelimiterCount is 1,
                    Color = Color.Default
                },
                PlainText = src.FirstChild.ToString()
            });

        CreateMap<LineBreakInline, RichTextBase>()
            .ConvertUsing((src, _, _) => new RichTextText
            {
                Text = new Text
                {
                    Content = Environment.NewLine
                },
                Annotations = new Annotations
                {
                    IsBold = false,
                    IsItalic = false,
                    IsStrikeThrough = false,
                    IsUnderline = false,
                    IsCode = false,
                    Color = Color.Default
                },
                PlainText = Environment.NewLine
            });
        
        CreateMap<CodeInline, RichTextBase>()
            .ConvertUsing((src, _, _) => new RichTextText
            {
                Text = new Text
                {
                    Content = src.Content
                },
                Annotations = new Annotations
                {
                    IsBold = false,
                    IsItalic = false,
                    IsStrikeThrough = false,
                    IsUnderline = false,
                    IsCode = true,
                    Color = Color.Default
                },
                PlainText = src.Content
            });
            
        // Paragraph block mapping
        CreateMap<ParagraphBlock, Block>()
            .ConvertUsing((src, _, context) => 
            {
                var richTexts = new List<RichTextBase>();
                
                foreach (var inline in src.Inline)
                {
                    var richText = context.Mapper.Map<RichTextBase>(inline);
                    if (richText != null)
                    {
                        richTexts.Add(richText);
                    }
                }
                
                return new Notion.Client.ParagraphBlock
                {
                    Paragraph = new Notion.Client.ParagraphBlock.Info
                    {
                        RichText = richTexts,
                        Color = Color.Default
                    }
                };
            });

        // Heading block mappings
        CreateMap<HeadingBlock, Block>()
            .ConvertUsing((src, _, context) => 
            {
                var richTexts = new List<RichTextBase>();
                
                foreach (var inline in src.Inline)
                {
                    var richText = context.Mapper.Map<RichTextBase>(inline);
                    if (richText != null)
                    {
                        richTexts.Add(richText);
                    }
                }
                
                // Map heading level to appropriate Notion heading block
                switch (src.Level)
                {
                    case 1:
                        return new HeadingOneBlock
                        {
                            Heading_1 = new HeadingOneBlock.Info
                            {
                                RichText = richTexts,
                                Color = Color.Default
                            }
                        };
                    case 2:
                        return new HeadingTwoBlock
                        {
                            Heading_2 = new HeadingTwoBlock.Info
                            {
                                RichText = richTexts,
                                Color = Color.Default
                            }
                        };
                    case 3:
                        return new HeadingThreeBlock
                        {
                            Heading_3 = new HeadingThreeBlock.Info
                            {
                                RichText = richTexts,
                                Color = Color.Default
                            }
                        };
                    default:
                        // For heading levels not directly supported by Notion, fallback to paragraph
                        return new HeadingThreeBlock
                        {
                            Heading_3 = new HeadingThreeBlock.Info
                            {
                                RichText = richTexts,
                                Color = Color.Default
                            }
                        };
                }
            });

        CreateMap<ListBlock, Block>()
            .ConvertUsing(MappingListBlock);
        
        // Quote block mapping
        CreateMap<QuoteBlock, Block>()
            .ConvertUsing((src, _, context) => 
            {
                var richTexts = new List<RichTextBase>();
                
                foreach (var block in src)
                {
                    if (block is ParagraphBlock paragraphBlock && paragraphBlock.Inline != null)
                    {
                        foreach (var inline in paragraphBlock.Inline)
                        {
                            var richText = context.Mapper.Map<RichTextBase>(inline);
                            if (richText != null)
                            {
                                richTexts.Add(richText);
                            }
                        }
                    }
                }
                
                return new Notion.Client.QuoteBlock
                {
                    Quote = new Notion.Client.QuoteBlock.Info
                    {
                        RichText = richTexts,
                        Color = Color.Default
                    }
                };
            });

        // // Code block mapping
        CreateMap<CodeBlock, Block>()
            .ConvertUsing((src, _, context) => 
            {
                var codeText = src.Lines.ToString();
                // Map language to a supported Notion code language or default to plain text
                var notionLanguage = MapToNotionCodeLanguage(string.Empty);
                
                return new Notion.Client.CodeBlock
                {
                    Code = new Notion.Client.CodeBlock.Info
                    {
                        RichText = new List<RichTextBase>
                        {
                            new RichTextText
                            {
                                Text = new Text
                                {
                                    Content = codeText
                                }
                            }
                        },
                        Language = notionLanguage
                    }
                };
            });
        CreateMap<FencedCodeBlock, Block>()
            .ConvertUsing((src, _, context) => 
            {
                var codeText = src.Lines.ToString();
                // Map language to a supported Notion code language or default to plain text
                var notionLanguage = MapToNotionCodeLanguage(src.Info ?? string.Empty);
                
                return new Notion.Client.CodeBlock
                {
                    Code = new Notion.Client.CodeBlock.Info
                    {
                        RichText = new List<RichTextBase>
                        {
                            new RichTextText
                            {
                                Text = new Text
                                {
                                    Content = codeText
                                }
                            }
                        },
                        Language = notionLanguage
                    }
                };
            });
        
        // ThematicBreak (horizontal rule) mapping
        CreateMap<ThematicBreakBlock, Block>()
            .ConvertUsing((_, _, _) => new DividerBlock
            {
                Divider = new DividerBlock.Data()
            });
        
        // Table mapping
        CreateMap<Table, Block>()
            .ConvertUsing((src, _, context) => 
            {
                var rows = new List<TableRowBlock>();
                
                foreach (var srcRow in src)
                {
                    if (srcRow is TableRow srcTableRow)
                    {
                        var cells = new List<List<RichTextText>>();
                        
                        foreach (var srcCell in srcTableRow)
                        {
                            if (srcCell is TableCell srcTableCell)
                            {
                                var cellRichTexts = new List<RichTextText>();
                                
                                foreach (var block in srcTableCell)
                                {
                                    if (block is ParagraphBlock paragraphBlock && paragraphBlock.Inline != null)
                                    {
                                        foreach (var inline in paragraphBlock.Inline)
                                        {
                                            var richText = context.Mapper.Map<RichTextBase>(inline);
                                            if (richText is RichTextText richTextText)
                                            {
                                                cellRichTexts.Add(richTextText);
                                            }
                                        }
                                    }
                                }
                                
                                cells.Add(cellRichTexts);
                            }
                        }
                        
                        rows.Add(new TableRowBlock
                        {
                            TableRow = new TableRowBlock.Info
                            {
                                Cells = cells
                            }
                        });
                    }
                }
                
                var hasHeaderRow = src.Count > 0 && src[0] is TableRow headerRow && headerRow.IsHeader;
                
                return new TableBlock
                {
                    Table = new TableBlock.Info
                    {
                        //idk why it is 1 greater column definitons than cells in a row.
                        TableWidth = src.ColumnDefinitions?.Count - 1 ?? 0,
                        HasColumnHeader = hasHeaderRow,
                        HasRowHeader = false,
                        Children = rows
                    }
                };
            });

        //TODO: still need to implment these:
        CreateMap<LinkReferenceDefinitionGroup, Block>()
            .ConvertUsing((_, _, _) => null);

        CreateMap<FootnoteGroup, Block>()
            .ConvertUsing((_, _, _) => null);

    }
    
    // Bulleted list mapping
    Block MappingListBlock(ListBlock src, Block _, ResolutionContext context)
    {
        // For bulleted list
        var items = context.Items["AllBlocks"] as List<Notion.Client.IBlock>;
        if (!src.IsOrdered)
        {
            var children = new List<BulletedListItemBlock>();
            foreach (var markDigBlock in src)
            {
                if (markDigBlock is ListItemBlock listItem)
                {
                    var richTexts = new List<RichTextBase>();

                    foreach (var block in listItem)
                    {
                        if (block is ParagraphBlock paragraphBlock && paragraphBlock.Inline != null)
                        {
                            foreach (var inline in paragraphBlock.Inline)
                            {
                                var richText = context.Mapper.Map<RichTextBase>(inline);
                                if (richText != null)
                                {
                                    richTexts.Add(richText);
                                }
                            }
                        }
                    }

                    children.Add(new BulletedListItemBlock {BulletedListItem = new BulletedListItemBlock.Info {RichText = richTexts, Color = Color.Default}});
                }
            }
            
            AddChildren(items, children);

            context.Items["AllBlocks"] = items;
            // return nothing because we are adding as children to previous block.
            return null;
        }
        else
        {
            var children = new List<NumberedListItemBlock>();
            foreach (var item in src)
            {
                if (item is ListItemBlock listItem)
                {
                    var richTexts = new List<RichTextBase>();

                    foreach (var block in listItem)
                    {
                        if (block is ParagraphBlock paragraphBlock && paragraphBlock.Inline != null)
                        {
                            foreach (var inline in paragraphBlock.Inline)
                            {
                                var richText = context.Mapper.Map<RichTextBase>(inline);
                                if (richText != null)
                                {
                                    richTexts.Add(richText);
                                }
                            }
                        }
                    }

                    children.Add(new NumberedListItemBlock {NumberedListItem = new NumberedListItemBlock.Info {RichText = richTexts, Color = Color.Default}});
                }
            }
            
            AddChildren(items, children);

            context.Items["AllBlocks"] = items;
            // return nothing because we are adding as children to previous block.
            return null;
        }
    }

    private void AddChildren<T>(List<IBlock> items, List<T> children) where T : Block
    {
        var lastItem = items.Last();
        switch (lastItem)
        {
            case Notion.Client.ParagraphBlock lastItemParagraphBlock:
                lastItemParagraphBlock.Paragraph.Children = children as IEnumerable<INonColumnBlock>;
                break;
            case BulletedListItemBlock lastItemBulletedListItemBlock:
                lastItemBulletedListItemBlock.BulletedListItem.Children = children as IEnumerable<INonColumnBlock>;
                break;
            case NumberedListItemBlock lastItemNumberedListItemBlock:
                lastItemNumberedListItemBlock.NumberedListItem.Children = children as IEnumerable<INonColumnBlock>;
                break;
            case Notion.Client.QuoteBlock lastItemQuoteBlock:
                lastItemQuoteBlock.Quote.Children = children as IEnumerable<INonColumnBlock>;
                break;
            default:
                items.AddRange(children);
                break;
        }
    }
    
    public static string MapToNotionCodeLanguage(string markdownLanguage)
    {
        // Map markdown language identifiers to Notion code block language options
        return markdownLanguage switch
        {
            "abap" => "abap",
            "agda" => "agda",
            "arduino" => "arduino",
            "ascii art" or "ascii" => "ascii art",
            "asm" or "assembly" => "assembly",
            "sh" or "bash" => "bash",
            "basic" => "basic",
            "bnf" => "bnf",
            "c" => "c",
            "c#" or "csharp" => "c#",
            "cpp" or "c++" => "c++",
            "clj" or "clojure" => "clojure",
            "coffee" or "coffeescript" => "coffeescript",
            "coq" => "coq",
            "css" => "css",
            "dart" => "dart",
            "dhall" => "dhall",
            "diff" => "diff",
            "dockerfile" or "docker" => "docker",
            "ebnf" => "ebnf",
            "elixir" => "elixir",
            "elm" => "elm",
            "erlang" => "erlang",
            "f#" or "fsharp" => "f#",
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
            "js" or "javascript" => "javascript",
            "json" => "json",
            "julia" => "julia",
            "kotlin" => "kotlin",
            "latex" => "latex",
            "less" => "less",
            "lisp" => "lisp",
            "livescript" => "livescript",
            "llvm" or "llvm ir" => "llvm ir",
            "lua" => "lua",
            "makefile" => "makefile",
            "md" or "markdown" => "markdown",
            "markup" => "markup",
            "matlab" => "matlab",
            "mathematica" => "mathematica",
            "mermaid" => "mermaid",
            "nix" => "nix",
            "notion-formula" or "notion formula" => "notion formula",
            "objc" or "objective-c" => "objective-c",
            "ocaml" => "ocaml",
            "pascal" => "pascal",
            "perl" => "perl",
            "php" => "php",
            "txt" or "text" => "plain text",
            "powershell" or "ps1" => "powershell",
            "prolog" => "prolog",
            "proto" or "protobuf" => "protobuf",
            "purescript" => "purescript",
            "py" or "python" => "python",
            "r" => "r",
            "racket" => "racket",
            "reason" => "reason",
            "rb" or "ruby" => "ruby",
            "rust" => "rust",
            "sass" => "sass",
            "scala" => "scala",
            "scheme" => "scheme",
            "scss" => "scss",
            "shell" => "shell",
            "smalltalk" => "smalltalk",
            "solidity" or "sol" => "solidity",
            "sql" => "sql",
            "swift" => "swift",
            "toml" => "toml",
            "ts" or "typescript" => "typescript",
            "vb" or "vb.net" => "vb.net",
            "verilog" => "verilog",
            "vhdl" => "vhdl",
            "visual basic" => "visual basic",
            "wasm" or "webassembly" => "webassembly",
            "xml" => "xml",
            "yaml" or "yml" => "yaml",
            "java/c/c++/c#" => "java/c/c++/c#",
            "notionscript" => "notionscript",
            _ => "plain text"
        };
    }
}