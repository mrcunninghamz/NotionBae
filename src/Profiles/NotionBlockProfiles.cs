using System.Collections.Generic;
using AutoMapper;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Notion.Client;
using Block = Notion.Client.Block;
using CodeBlock = Markdig.Syntax.CodeBlock;
using ParagraphBlock = Markdig.Syntax.ParagraphBlock;
using QuoteBlock = Markdig.Syntax.QuoteBlock;

namespace NotionBae.Profiles;

public class NotionBlockProfiles : Profile
{
    public NotionBlockProfiles()
    {
        // Map from Markdig MarkdownDocument to a list of Notion Blocks
        CreateMap<MarkdownDocument, List<Notion.Client.IBlock>>()
            .ConvertUsing((doc, _, context) => 
            {
                var blocks = new List<Notion.Client.IBlock>();
                context.Items["AllBlocks"] = blocks;
                foreach (var block in doc)
                {
                    var mappedBlock = context.Mapper.Map<Notion.Client.Block>(block);
                    if (mappedBlock != null)
                    {
                        blocks.Add(mappedBlock);
                    }
                }
                return blocks;
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
                    Content = string.Empty
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
                PlainText = String.Empty
            });
        
        CreateMap<CodeInline, RichTextBase>()
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
                    IsCode = true,
                    Color = Color.Default
                },
                PlainText = src.ToString()
            });
            
        // Paragraph block mapping
        CreateMap<ParagraphBlock, Notion.Client.Block>()
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
        CreateMap<HeadingBlock, Notion.Client.Block>()
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
                        return new HeadingTwoBlock()
                        {
                            Heading_2 = new HeadingTwoBlock.Info
                            {
                                RichText = richTexts,
                                Color = Color.Default
                            }
                        };
                    case 3:
                        return new HeadingThreeBlock()
                        {
                            Heading_3 = new HeadingThreeBlock.Info
                            {
                                RichText = richTexts,
                                Color = Color.Default
                            }
                        };
                    default:
                        // For heading levels not directly supported by Notion, fallback to paragraph
                        return new HeadingThreeBlock()
                        {
                            Heading_3 = new HeadingThreeBlock.Info
                            {
                                RichText = richTexts,
                                Color = Color.Default
                            }
                        };
                }
            });

        CreateMap<ListBlock, Notion.Client.Block>()
            .ConvertUsing(MappingListBlock);
        
        // Quote block mapping
        CreateMap<QuoteBlock, Notion.Client.Block>()
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
                var language = string.IsNullOrEmpty(src.) ? "plain text" : src.Info.ToLower();
                
                // Map language to a supported Notion code language or default to plain text
                var notionLanguage = MapToNotionCodeLanguage(language);
                
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
        //
        //
        // // ThematicBreak (horizontal rule) mapping
        // CreateMap<ThematicBreakBlock, Block>()
        //     .ConvertUsing((_, _, _) => new DividerBlock
        //     {
        //         Divider = new DividerBlock.Info() 
        //     });
        //
        // // Table mapping
        // CreateMap<Table, Block>()
        //     .ConvertUsing((src, _, context) => 
        //     {
        //         var rows = new List<TableRow>();
        //         
        //         foreach (var rowObj in src)
        //         {
        //             if (rowObj is TableRow tableRow)
        //             {
        //                 var cells = new List<List<RichTextBase>>();
        //                 
        //                 foreach (var cellObj in tableRow)
        //                 {
        //                     if (cellObj is TableCell tableCell)
        //                     {
        //                         var cellRichTexts = new List<RichTextBase>();
        //                         
        //                         foreach (var block in tableCell)
        //                         {
        //                             if (block is ParagraphBlock paragraphBlock && paragraphBlock.Inline != null)
        //                             {
        //                                 foreach (var inline in paragraphBlock.Inline)
        //                                 {
        //                                     var richText = context.Mapper.Map<RichTextBase>(inline);
        //                                     if (richText != null)
        //                                     {
        //                                         cellRichTexts.Add(richText);
        //                                     }
        //                                 }
        //                             }
        //                         }
        //                         
        //                         cells.Add(cellRichTexts);
        //                     }
        //                 }
        //                 
        //                 rows.Add(new TableRow
        //                 {
        //                     Cells = cells
        //                 });
        //             }
        //         }
        //         
        //         var hasHeaderRow = src.Count > 0 && src[0] is TableRow headerRow && headerRow.IsHeader;
        //         
        //         return new TableBlock
        //         {
        //             Table = new TableBlock.Info
        //             {
        //                 TableWidth = src.ColumnDefinitions?.Count ?? 0,
        //                 HasColumnHeader = hasHeaderRow,
        //                 HasRowHeader = false,
        //                 Children = rows
        //             }
        //         };
        //     });
        //
        // // Inline mappings to RichTextBase
        // CreateMap<LiteralInline, RichTextBase>()
        //     .ConvertUsing((src, _, _) => new RichTextText
        //     {
        //         Text = new Text
        //         {
        //             Content = src.Content.ToString()
        //         }
        //     });
        //
        // CreateMap<EmphasisInline, RichTextBase>()
        //     .ConvertUsing((src, _, context) => 
        //     {
        //         var content = "";
        //         foreach (var inline in src)
        //         {
        //             if (inline is LiteralInline literalInline)
        //             {
        //                 content += literalInline.Content.ToString();
        //             }
        //         }
        //         
        //         var richText = new RichTextText
        //         {
        //             Text = new Text
        //             {
        //                 Content = content
        //             },
        //             Annotations = new Annotations()
        //         };
        //         
        //         // Apply appropriate formatting based on delimiter
        //         if (src.DelimiterChar == '*' || src.DelimiterChar == '_')
        //         {
        //             if (src.DelimiterCount == 1)
        //             {
        //                 richText.Annotations.Italic = true;
        //             }
        //             else if (src.DelimiterCount == 2)
        //             {
        //                 richText.Annotations.Bold = true;
        //             }
        //             else if (src.DelimiterCount == 3)
        //             {
        //                 richText.Annotations.Bold = true;
        //                 richText.Annotations.Italic = true;
        //             }
        //         }
        //         
        //         return richText;
        //     });
        //
        // CreateMap<LinkInline, RichTextBase>()
        //     .ConvertUsing((src, _, _) => new RichTextText
        //     {
        //         Text = new Text
        //         {
        //             Content = src.Title ?? src.Url,
        //             Link = new Link
        //             {
        //                 Url = src.Url
        //             }
        //         }
        //     });
        //
        // CreateMap<CodeInline, RichTextBase>()
        //     .ConvertUsing((src, _, _) => new RichTextText
        //     {
        //         Text = new Text
        //         {
        //             Content = src.Content.ToString()
        //         },
        //         Annotations = new Annotations
        //         {
        //             Code = true
        //         }
        //     });
    }
    
    // Bulleted list mapping
    Block MappingListBlock(ListBlock src, Block _, ResolutionContext context)
    {
        // For bulleted list
        var items = context.Items["AllBlocks"] as List<Notion.Client.IBlock>;
        var lastItem = items?.LastOrDefault();
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
            
            AddChildren(lastItem!, children);

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
            
            AddChildren(lastItem!, children);

            context.Items["AllBlocks"] = items;
            // return nothing because we are adding as children to previous block.
            return null;
        }
    }

    private void AddChildren<T>(Notion.Client.IBlock lastItem, List<T> children) where T : Block
    {
        switch (lastItem)
        {
            case Notion.Client.ParagraphBlock lastItemParagraphBlock:
                lastItemParagraphBlock.Paragraph.Children = children as IEnumerable<INonColumnBlock>;
                break;
            case Notion.Client.BulletedListItemBlock lastItemBulletedListItemBlock:
                lastItemBulletedListItemBlock.BulletedListItem.Children = children as IEnumerable<INonColumnBlock>;
                break;
            case Notion.Client.NumberedListItemBlock lastItemNumberedListItemBlock:
                lastItemNumberedListItemBlock.NumberedListItem.Children = children as IEnumerable<INonColumnBlock>;
                break;
            case Notion.Client.QuoteBlock lastItemQuoteBlock:
                lastItemQuoteBlock.Quote.Children = children as IEnumerable<INonColumnBlock>;
                break;
        }
    }
    
    private string MapToNotionCodeLanguage(string markdownLanguage)
    {
        // Map markdown language identifiers to Notion code block language options
        return markdownLanguage switch
        {
            "c#" or "csharp" => "c#",
            "js" or "javascript" => "javascript",
            "ts" or "typescript" => "typescript",
            "java" => "java",
            "py" or "python" => "python",
            "rb" or "ruby" => "ruby",
            "go" => "go",
            "php" => "php",
            "rust" => "rust",
            "cpp" or "c++" => "c++",
            "c" => "c",
            "sh" or "bash" => "shell",
            "html" => "html",
            "css" => "css",
            "sql" => "sql",
            "json" => "json",
            "yaml" or "yml" => "yaml",
            "swift" => "swift",
            "kotlin" => "kotlin",
            "xml" => "xml",
            _ => "plain text"
        };
    }
}