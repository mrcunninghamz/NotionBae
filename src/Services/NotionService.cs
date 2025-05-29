using System.Text.Json;
using AutoMapper;
using Markdig;
using Markdig.Renderers.Normalize;
using Markdig.Syntax;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notion.Client;
using NotionBae.Utilities;
using Block = Notion.Client.Block;
using IBlock = Notion.Client.IBlock;
using ParagraphBlock = Notion.Client.ParagraphBlock;

namespace NotionBae.Services;

public interface INotionService
{
    Task<SearchResponse> Search(string query);
    Task<Page> CreatePage(string parentId, string title, string content);
    Task<Page> RetrievePage(string pageId);
    Task<RetrieveChildrenResponse> RetrieveBlockChildren(string blockId);
    Task<Page> UpdatePage(string pageId, string title);
    Task<IBlock> UpdateBlock(string blockId, string content);
    Task DeleteBlock(string blockId);
    Task DeleteBlocks(List<string> blockIds);
    Task<AppendChildrenResponse> AppendBlockChildren(string blockId, string content, string? after = null);
    Task<string> GetPageContent(string blockId);
}

public class NotionService : INotionService
{
    private readonly ILogger<NotionService> _logger;
    private readonly IMapper _mapper;
    private readonly INotionClient _client;

    public NotionService(INotionClient client, ILogger<NotionService> logger, IMapper mapper)
    {
        _logger = logger;
        _mapper = mapper;
        _client = client;
    }
    
    public async Task<SearchResponse> Search(string query)
    {
        var payload = new SearchRequest
        {
            Query = query
        };

        return await _client.Search.SearchAsync(payload);
    }
    
    public async Task<Page> CreatePage(string parentId, string title, string content)
    {
        var blocks = MarkdownToNotion(content);
        var pagesCreateParameters = PagesCreateParametersBuilder
            .Create(new ParentPageInput
            {
                PageId = parentId
            })
            .AddProperty("title", new TitlePropertyValue
            {
                Title = new List<RichTextBase> {new RichTextText {Text = new Text {Content = title}}}
            });
        
        var page = pagesCreateParameters.Build();
        page.Children = blocks;

        return await _client.Pages.CreateAsync(page);
    }

    public async Task<Page> RetrievePage(string pageId)
    {
        return await _client.Pages.RetrieveAsync(pageId);
    }

    public async Task<RetrieveChildrenResponse> RetrieveBlockChildren(string blockId)
    {
        return await _client.Blocks.RetrieveChildrenAsync(new BlockRetrieveChildrenRequest
        {
            BlockId = blockId
        });
    }

    public async Task<Page> UpdatePage(string pageId, string title)
    {
        return await _client.Pages.UpdateAsync(pageId,
            new PagesUpdateParameters
            {
                Properties = new Dictionary<string, PropertyValue>
                {
                    {
                        "title", new TitlePropertyValue
                        {
                            Title = new List<RichTextBase>
                            {
                                new RichTextText
                                {
                                    Text = new Text
                                    {
                                        Content = title
                                    }
                                }
                            }
                        }
                    }
                }
            });
    }
    
    public async Task<IBlock> UpdateBlock(string blockId, string content)
    {
        var blocks = MarkdownToNotionUpdate(content);
        return await _client.Blocks.UpdateAsync(blockId, blocks);
    }

    public async Task DeleteBlock(string blockId)
    {
        _logger.LogInformation("Deleting block with ID: {BlockId}", blockId);

        await _client.Blocks.DeleteAsync(blockId);
    }

    public async Task DeleteBlocks(List<string> blockIds)
    {
        var tasks = new List<Task>();
        foreach (var blockId in blockIds)
        {
            tasks.Add(Task.Run(async () => await DeleteBlock(blockId)));
        }

        try
        {
            await Task.WhenAll(tasks);
            _logger.LogInformation("Successfully deleted existing blocks");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception occurred while deleting existing blocks");
            throw;
        }
    }
    
    public async Task<AppendChildrenResponse> AppendBlockChildren(string blockId, string content, string? after = null)
    {
        _logger.LogInformation("Appending children to block with ID: {BlockId}", blockId);

        var blocks = MarkdownToNotionAppend(content);
        
        var blockAppendChildrenRequest = new BlockAppendChildrenRequest
        {
            BlockId = blockId,
            Children = blocks,
            After = after
        };

        return await _client.Blocks.AppendChildrenAsync(blockAppendChildrenRequest);
    }

    public List<IBlock> MarkdownToNotion(string content)
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var documents = Markdown.Parse(content, pipeline);
        
        var blocks = _mapper.Map<List<IBlock>>(documents, opt => 
        {
            opt.Items["AllBlocks"] = new List<BlockObjectRequest>();
            opt.Items["Parent"] = null;
        });

        return blocks;
    }

    public IUpdateBlock MarkdownToNotionUpdate(string content)
    {
        var blocks = MarkdownToNotion(content).FirstOrDefault();
        return _mapper.Map<IUpdateBlock>(blocks);
    }
    
    
    public List<IBlockObjectRequest> MarkdownToNotionAppend(string content)
    {
        var blocks = MarkdownToNotion(content);

        return _mapper.Map<List<IBlockObjectRequest>>(blocks);
    }
    
    public string NotionToMarkdown(List<IBlock> blocks)
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var markdown = _mapper.Map<MarkdownDocument>(blocks, opt =>
        {
            opt.Items["AllBlocks"] = new MarkdownDocument();
            opt.Items["Parent"] = null;
        });
        
        var writer = new StringWriter();
        var renderer = new NormalizeRenderer(writer);
        renderer.ObjectRenderers.Add(new TableRenderer());
        renderer.CompactParagraph = true;
        pipeline.Setup(renderer);

        renderer.Render(markdown);
        writer.Flush();
        
        return writer.ToString();
    }
    
    public async Task<string> GetPageContent(string blockId)
    {
        var notionBlocks = await GetPageContentWithChildren(blockId);

        return NotionToMarkdown(notionBlocks);
    }
    
    private async Task<List<IBlock>> GetPageContentWithChildren(string blockId)
    {
        var allBlocks = new List<IBlock>();
        var blockQueue = new Queue<(string BlockId, IBlock? ParentBlock)>();
    
        // Start with the root block
        blockQueue.Enqueue((blockId, null));
    
        while (blockQueue.Count > 0)
        {
            var (currentBlockId, parentBlock) = blockQueue.Dequeue();
            var childrenResponse = await RetrieveBlockChildren(currentBlockId);
        
            foreach (var block in childrenResponse.Results)
            {
                if (parentBlock != null)
                {
                    // Add as child to parent block
                    AddChildren(parentBlock, block);
                }
                else
                {
                    // Add as top-level block
                    allBlocks.Add(block);
                }
            
                // If this block has children, add them to the queue
                if (block.HasChildren)
                {
                    blockQueue.Enqueue((block.Id, block));
                }
            }
        }
        
        return allBlocks;
    }
    
    //TODO: probably fingure out what else has children and make sure we pull them in here.
    private void AddChildren(IBlock parent, IBlock child)
    {
        switch (parent)
        {
            case TableBlock tableBlock:
                if(tableBlock.Table.Children == null)
                {
                    tableBlock.Table.Children = new List<TableRowBlock>();
                }
                tableBlock.Table.Children = tableBlock.Table.Children.Append(child as TableRowBlock);
                break;
            case BulletedListItemBlock bulletedListItemBlock:
                if(bulletedListItemBlock.BulletedListItem.Children == null)
                {
                    bulletedListItemBlock.BulletedListItem.Children = new List<BulletedListItemBlock>();
                }
                bulletedListItemBlock.BulletedListItem.Children = bulletedListItemBlock.BulletedListItem.Children.Append(child as INonColumnBlock);
                break;
            case NumberedListItemBlock numberedListItemBlock:
                if(numberedListItemBlock.NumberedListItem.Children == null)
                {
                    numberedListItemBlock.NumberedListItem.Children = new List<NumberedListItemBlock>();
                }
                numberedListItemBlock.NumberedListItem.Children = numberedListItemBlock.NumberedListItem.Children.Append(child as INonColumnBlock);
                break;
            case ParagraphBlock paragraphBlock:
                if(paragraphBlock.Paragraph.Children == null)
                {
                    paragraphBlock.Paragraph.Children = new List<ParagraphBlock>();
                }
                paragraphBlock.Paragraph.Children = paragraphBlock.Paragraph.Children.Append(child as INonColumnBlock);
                break;
                
        }
    }
}