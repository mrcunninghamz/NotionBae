using AutoMapper;
using Markdig.Extensions.Footnotes;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Notion.Client;
using CodeBlock = Markdig.Syntax.CodeBlock;
using ParagraphBlock = Markdig.Syntax.ParagraphBlock;
using QuoteBlock = Markdig.Syntax.QuoteBlock;


namespace NotionBae.Profiles;

public class MdToBlockObjectRequestProfile : Profile
{
    public MdToBlockObjectRequestProfile()
    {
        // Map from Markdig MarkdownDocument to a list of Notion Blocks
        CreateMap<MarkdownDocument, List<IBlockObjectRequest>>()
            .ConvertUsing((doc, _, context) => 
            {
                var blocks = new List<IBlockObjectRequest>();
                context.Items["AllBlocks"] = blocks;
                foreach (var block in doc)
                {
                    var mappedBlock = context.Mapper.Map<IBlockObjectRequest>(block);
                    if (mappedBlock != null)
                    {
                        blocks.Add(mappedBlock);
                    }
                }
                return blocks;
            });
            
        // Paragraph block mapping
        CreateMap<ParagraphBlock, IBlockObjectRequest>()
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
                
                return new ParagraphBlockRequest
                {
                    Paragraph = new ParagraphBlockRequest.Info
                    {
                        RichText = richTexts,
                        Color = Color.Default
                    }
                };
            });

        // Heading block mappings
        CreateMap<HeadingBlock, IBlockObjectRequest>()
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
                        return new HeadingOneBlockRequest
                        {
                            Heading_1 = new HeadingOneBlockRequest.Info
                            {
                                RichText = richTexts,
                                Color = Color.Default
                            }
                        };
                    case 2:
                        return new HeadingTwoBlockRequest
                        {
                            Heading_2 = new HeadingTwoBlockRequest.Info
                            {
                                RichText = richTexts,
                                Color = Color.Default
                            }
                        };
                    case 3:
                        return new HeadingThreeBlockRequest
                        {
                            Heading_3 = new HeadingThreeBlockRequest.Info
                            {
                                RichText = richTexts,
                                Color = Color.Default
                            }
                        };
                    default:
                        // For heading levels not directly supported by Notion, fallback to paragraph
                        return new HeadingThreeBlockRequest
                        {
                            Heading_3 = new HeadingThreeBlockRequest.Info
                            {
                                RichText = richTexts,
                                Color = Color.Default
                            }
                        };
                }
            });

        CreateMap<ListBlock, IBlockObjectRequest>()
            .ConvertUsing(MappingListBlock);
        
        // Quote block mapping
        CreateMap<QuoteBlock, IBlockObjectRequest>()
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
                
                return new QuoteBlockRequest
                {
                    Quote = new QuoteBlockRequest.Info
                    {
                        RichText = richTexts,
                        Color = Color.Default
                    }
                };
            });

        // // Code block mapping
        CreateMap<CodeBlock, IBlockObjectRequest>()
            .ConvertUsing((src, _, context) => 
            {
                var codeText = src.Lines.ToString();
                // Map language to a supported Notion code language or default to plain text
                var notionLanguage = MdToNotionBlockProfile.MapToNotionCodeLanguage(string.Empty);
                
                return new CodeBlockRequest
                {
                    Code = new CodeBlockRequest.Info
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
        CreateMap<FencedCodeBlock, IBlockObjectRequest>()
            .ConvertUsing((src, _, context) => 
            {
                var codeText = src.Lines.ToString();
                // Map language to a supported Notion code language or default to plain text
                var notionLanguage = MdToNotionBlockProfile.MapToNotionCodeLanguage(src.Info ?? string.Empty);
                
                return new CodeBlockRequest
                {
                    Code = new CodeBlockRequest.Info
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
        CreateMap<ThematicBreakBlock, IBlockObjectRequest>()
            .ConvertUsing((_, _, _) => new DividerBlockRequest
            {
                Divider = new DividerBlockRequest.Data()
            });
        
        // Table mapping
        CreateMap<Table, IBlockObjectRequest>()
            .ConvertUsing((src, _, context) => 
            {
                var rows = new List<TableRowBlockRequest>();
                
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
                        
                        rows.Add(new TableRowBlockRequest
                        {
                            TableRow = new TableRowBlockRequest.Info
                            {
                                Cells = cells
                            }
                        });
                    }
                }
                
                var hasHeaderRow = src.Count > 0 && src[0] is TableRow headerRow && headerRow.IsHeader;
                
                return new TableBlockRequest
                {
                    Table = new TableBlockRequest.Info
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
        CreateMap<LinkReferenceDefinitionGroup, IBlockObjectRequest>()
            .ConvertUsing((_, _, _) => null);

        CreateMap<FootnoteGroup, IBlockObjectRequest>()
            .ConvertUsing((_, _, _) => null);
    }
    
    // Bulleted list mapping
    IBlockObjectRequest MappingListBlock(ListBlock src, IBlockObjectRequest _, ResolutionContext context)
    {
        // For bulleted list
        var items = context.Items["AllBlocks"] as List<IBlockObjectRequest>;
        if (!src.IsOrdered)
        {
            var (richTextBases, listBlock) = NotionBlockProfile.GenerateChildren(src, context, items!);

            if (listBlock != null)
            {
                AddBlockChildren(
                    items!,
                    richTextBases.Select(richTexts => new BulletedListItemBlockRequest {BulletedListItem = new BulletedListItemBlockRequest.Info {RichText = richTexts, Color = Color.Default}})
                );
                context.Mapper.Map<IBlockObjectRequest>(listBlock);
                return null;
            }
            
            AddBlockChildren(
                items!,
                richTextBases.Select(richTexts => new BulletedListItemBlockRequest {BulletedListItem = new BulletedListItemBlockRequest.Info {RichText = richTexts, Color = Color.Default}})
            );
            
            context.Items["AllBlocks"] = items;
        }
        else
        {
            var (richTextBases, listBlock) = NotionBlockProfile.GenerateChildren(src, context, items!);

            if (listBlock != null)
            {
                AddBlockChildren(
                    items!,
                    richTextBases.Select(richTexts => new NumberedListItemBlockRequest {NumberedListItem = new NumberedListItemBlockRequest.Info {RichText = richTexts, Color = Color.Default}})
                );
                context.Mapper.Map<IBlockObjectRequest>(listBlock);
                return null;
            }
            
            AddBlockChildren(
                items!,
                richTextBases.Select(richTexts => new NumberedListItemBlockRequest {NumberedListItem = new NumberedListItemBlockRequest.Info {RichText = richTexts, Color = Color.Default}})
            );
            
            context.Items["AllBlocks"] = items;
        }
            
        // return nothing because we are adding as children to previous block.
        return null;
    }
    
    private void AddBlockChildren<T>(List<IBlockObjectRequest> items, IEnumerable<T> children) where T : IBlockObjectRequest
    {
        var lastItem = items.Last();
        switch (lastItem)
        {
            case ParagraphBlockRequest lastItemParagraphBlock:
                lastItemParagraphBlock.Paragraph.Children = children as IEnumerable<INonColumnBlockRequest>;
                break;
            case BulletedListItemBlockRequest lastItemBulletedListItemBlock:
                lastItemBulletedListItemBlock.BulletedListItem.Children = children as IEnumerable<BulletedListItemBlockRequest>;
                break;
            case NumberedListItemBlockRequest lastItemNumberedListItemBlock:
                lastItemNumberedListItemBlock.NumberedListItem.Children = children as IEnumerable<INonColumnBlockRequest>;
                break;
            case QuoteBlockRequest lastItemQuoteBlock:
                lastItemQuoteBlock.Quote.Children = children as IEnumerable<INonColumnBlockRequest>;
                break;
            default:
                var insertChidlren = children as IEnumerable<IBlockObjectRequest>;
                if (insertChidlren != null)
                {
                    items.AddRange(insertChidlren);
                }
                break;
        }
    }
    
}
