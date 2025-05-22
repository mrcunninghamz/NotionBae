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
    }
}
