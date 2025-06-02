using AutoMapper;
using Notion.Client;

namespace NotionBae.Profiles;

public class NotionProfile : Profile
{
    public NotionProfile()
    {
        CreateMap<List<RichTextBase>, BulletedListItemBlock>()
            .ConvertUsing((src, _, context) => new BulletedListItemBlock
            {
                BulletedListItem = new BulletedListItemBlock.Info
                {
                    RichText = src,
                    Color = Color.Default
                }
            });
        
        CreateMap<List<RichTextBase>, NumberedListItemBlock>()
            .ConvertUsing((src, _, context) => new NumberedListItemBlock
            {
                NumberedListItem = new NumberedListItemBlock.Info
                {
                    RichText = src,
                    Color = Color.Default
                }
            });
        CreateMap<List<RichTextBase>, BulletedListItemBlockRequest>()
            .ConvertUsing((src, _, context) => new BulletedListItemBlockRequest
            {
                BulletedListItem = new BulletedListItemBlockRequest.Info
                {
                    RichText = src,
                    Color = Color.Default
                }
            });
        
        CreateMap<List<RichTextBase>, NumberedListItemBlockRequest>()
            .ConvertUsing((src, _, context) => new NumberedListItemBlockRequest
            {
                NumberedListItem = new NumberedListItemBlockRequest.Info
                {
                    RichText = src,
                    Color = Color.Default
                }
            });


        CreateMap<RichTextBase, RichTextBaseInput>();
        CreateMap<RichTextText, RichTextTextInput>()
            .IncludeBase<RichTextBase, RichTextBaseInput>();
        
        // Map IBlock to IUpdateBlock
        CreateMap<IBlock, IUpdateBlock>();
        
        CreateMap<HeadingOneBlock, HeadingOneUpdateBlock>()
            .IncludeBase<IBlock, IUpdateBlock>();
        CreateMap<HeadingOneBlock.Info, HeadingOneUpdateBlock.Info>();
        
        CreateMap<HeadingTwoBlock, HeadingTwoUpdateBlock>()
            .IncludeBase<IBlock, IUpdateBlock>();
        CreateMap<HeadingTwoBlock.Info, HeadingTwoUpdateBlock.Info>();
        
        CreateMap<HeadingThreeBlock, HeadingThreeUpdateBlock>()
            .IncludeBase<IBlock, IUpdateBlock>();
        CreateMap<HeadingThreeBlock.Info, HeadingThreeUpdateBlock.Info>();
        
        CreateMap<ParagraphBlock, ParagraphUpdateBlock>()
            .IncludeBase<IBlock, IUpdateBlock>();
        CreateMap<ParagraphBlock.Info, ParagraphUpdateBlock.Info>();
        
        CreateMap<QuoteBlock, QuoteUpdateBlock>()
            .IncludeBase<IBlock, IUpdateBlock>();
        CreateMap<QuoteBlock.Info, QuoteUpdateBlock.Info>();

        CreateMap<BulletedListItemBlock, BulletedListItemUpdateBlock>()
            .IncludeBase<IBlock, IUpdateBlock>();
        CreateMap<BulletedListItemBlock.Info, BulletedListItemUpdateBlock.Info>()
            .ConvertUsing((src, _, context) => new BulletedListItemUpdateBlock.Info
            {
                RichText = context.Mapper.Map<List<RichTextTextInput>>(src.RichText),
            });

        CreateMap<NumberedListItemBlock, NumberedListItemUpdateBlock>()
            .IncludeBase<IBlock, IUpdateBlock>();
        CreateMap<NumberedListItemBlock.Info, NumberedListItemUpdateBlock.Info>()
            .ConvertUsing((src, _, context) => new NumberedListItemUpdateBlock.Info
            {
                RichText = context.Mapper.Map<List<RichTextTextInput>>(src.RichText),
            });

        CreateMap<CodeBlock, CodeUpdateBlock>()
            .IncludeBase<IBlock, IUpdateBlock>();
        CreateMap<CodeBlock.Info, CodeUpdateBlock.Info>();

        CreateMap<DividerBlock, DividerUpdateBlock>()
            .IncludeBase<IBlock, IUpdateBlock>();
        CreateMap<DividerBlock.Data, DividerUpdateBlock.Info>();
        
        CreateMap<TableBlock, TableUpdateBlock>()
            .IncludeBase<IBlock, IUpdateBlock>();
        CreateMap<TableBlock.Info, TableUpdateBlock.Info>();
        CreateMap<TableRowBlock, TableRowUpdateBlock>()
            .IncludeBase<IBlock, IUpdateBlock>();
        CreateMap<TableRowBlock.Info, TableRowUpdateBlock.Info>();

        // Map IBlock to IBlockObjectRequest
        CreateMap<IBlock, IBlockObjectRequest>();
        CreateMap<INonColumnBlock, INonColumnBlockRequest>();
        
        CreateMap<HeadingOneBlock, HeadingOneBlockRequest>()
            .IncludeBase<IBlock, IBlockObjectRequest>();
        CreateMap<HeadingOneBlock.Info, HeadingOneBlockRequest.Info>();
        
        CreateMap<HeadingTwoBlock, HeadingTwoBlockRequest>()
            .IncludeBase<IBlock, IBlockObjectRequest>();
        CreateMap<HeadingTwoBlock.Info, HeadingTwoBlockRequest.Info>();
        
        CreateMap<HeadingThreeBlock, HeadingThreeBlockRequest>()
            .IncludeBase<IBlock, IBlockObjectRequest>();
        CreateMap<HeadingThreeBlock.Info, HeadingThreeBlockRequest.Info>();
        
        CreateMap<ParagraphBlock, ParagraphBlockRequest>()
            .IncludeBase<IBlock, IBlockObjectRequest>();
        CreateMap<ParagraphBlock.Info, ParagraphBlockRequest.Info>();
        
        CreateMap<QuoteBlock, QuoteBlockRequest>()
            .IncludeBase<IBlock, IBlockObjectRequest>();
        CreateMap<QuoteBlock.Info, QuoteBlockRequest.Info>();

        CreateMap<BulletedListItemBlock, BulletedListItemBlockRequest>()
            .IncludeBase<IBlock, IBlockObjectRequest>();
        CreateMap<BulletedListItemBlock.Info, BulletedListItemBlockRequest.Info>()
            .ConvertUsing(BulletedListChildrenConverter);

        CreateMap<NumberedListItemBlock, NumberedListItemBlockRequest>()
            .IncludeBase<IBlock, IBlockObjectRequest>();
        CreateMap<NumberedListItemBlock.Info, NumberedListItemBlockRequest.Info>()
            .ConvertUsing(NumberedListChildrenConverter);

        CreateMap<CodeBlock, CodeBlockRequest>()
            .IncludeBase<IBlock, IBlockObjectRequest>();
        CreateMap<CodeBlock.Info, CodeBlockRequest.Info>();

        CreateMap<DividerBlock, DividerBlockRequest>()
            .IncludeBase<IBlock, IBlockObjectRequest>();
        CreateMap<DividerBlock.Data, DividerBlockRequest.Data>();
        
        CreateMap<TableBlock, TableBlockRequest>()
            .IncludeBase<IBlock, IBlockObjectRequest>();
        CreateMap<TableBlock.Info, TableBlockRequest.Info>();
        CreateMap<TableRowBlock, TableRowBlockRequest>()
            .IncludeBase<IBlock, IBlockObjectRequest>();
        CreateMap<TableRowBlock.Info, TableRowBlockRequest.Info>();

        CreateMap<ChildPageBlock, ChildPageBlockRequest>()
            .IncludeBase<IBlock, IBlockObjectRequest>();
        CreateMap<ChildPageBlock.Info, ChildPageBlockRequest.Info>();
    }
    private NumberedListItemBlockRequest.Info NumberedListChildrenConverter(NumberedListItemBlock.Info src, NumberedListItemBlockRequest.Info _, ResolutionContext context)
    {
        var dest = new NumberedListItemBlockRequest.Info
        {
            RichText = src.RichText,
            Color = src.Color,
            Children = MapChildren(src.Children, context)
        };

        return dest;
    }
    private BulletedListItemBlockRequest.Info BulletedListChildrenConverter(BulletedListItemBlock.Info src, BulletedListItemBlockRequest.Info _, ResolutionContext context)
    {
        var dest = new BulletedListItemBlockRequest.Info
        {
            RichText = src.RichText,
            Color = src.Color,
            Children = MapChildren(src.Children, context)
        };

        return dest;
    }
    
    private List<INonColumnBlockRequest> MapChildren(IEnumerable<INonColumnBlock> childrenToAdd, ResolutionContext context)
    {
        var response = new List<INonColumnBlockRequest>();
        if (childrenToAdd == null) return response;

        foreach (var child in childrenToAdd)
        {
            switch (child)
            {
                case ParagraphBlock childParagraphBlock:
                    response.Add(context.Mapper.Map<ParagraphBlockRequest>(childParagraphBlock));
                    break;
                case BulletedListItemBlock childBlock:
                    response.Add(context.Mapper.Map<BulletedListItemBlockRequest>(childBlock));
                    break;
                case NumberedListItemBlock childNumbered:
                    response.Add(context.Mapper.Map<NumberedListItemBlockRequest>(childNumbered));
                    break;
                case QuoteBlock childQuoteBlock:
                    response.Add(context.Mapper.Map<QuoteBlockRequest>(childQuoteBlock));
                    break;
                // Add more cases as needed
            }
        }

        return response;
    }
}

