using NotionBae.Utilities;
using Xunit;

namespace NotionBae.Tests.Utilities
{
    public class MarkdownToNotionConverterTests
    {
        [Fact]
        public void ConvertToNotionBlocks_ConvertRoadmapMarkdown_ProducesCorrectBlockStructure()
        {
            // Arrange
            string markdown = @"# NotionBae Roadmap

This roadmap outlines the planned features and improvements for NotionBae, prioritized by importance and potential impact.

## Completed Features ✅

### Update Page Functionality ✅
- **Description**: Add ability to update existing Notion pages
- **Status**: Completed
- **Delivery Date**: Q1 2025

## High Priority Items

### Fix Markdown to Notion Conversion for Indentation
- **Description**: Improve handling of indented content like nested lists
- **Status**: In progress (some fixes have been made and should be in main, but still need to keep this top of mind)
- **Target Date**: Q2 2025
- **Details**:
  - Update MarkdownToNotionConverter to respect tab indentation
  - Implement proper nesting of blocks based on indentation level
  - Ensure numbered lists maintain proper hierarchy

## Medium Priority Items

### Database Integration
- **Description**: Add support for Notion databases
- **Status**: Not started
- **Target Date**: Q3 2025
- **Details**:
  - Create/update database records
  - Query database contents
  - Filter and sort results

### Support for More Block Types
- **Description**: Add support for additional Notion block types
- **Status**: Not started
- **Target Date**: Q3 2025
- **Details**:
  - Link previews (currently links are not even working from markdown)
  - Tables
  - Callouts
  - Toggles
  - Equations

## Low Priority Items

### Parallel Block Operations & Rate Limiting
- **Description**: Improve performance and reliability when dealing with Notion API rate limits
- **Status**: Not started
- **Target Date**: Q4 2025
- **Details**:
  - Implement parallel block deletion in UpdatePageContent
  - Add Polly policies for HTTP client to handle rate limiting
  - Implement bulkhead pattern to manage concurrent requests
  - Add retry and circuit breaker policies for resilience

## Future Considerations

### User Interface
- Simple GUI for interacting with NotionBae outside of MCP
- Visual block editor
- Target: 2026

### Authentication Improvements
- OAuth implementation for broader access
- Multi-user support
- Target: 2026

### Performance Optimization
- Batch operations for bulk updates
- Caching for frequently accessed pages
- Target: 2026";

            // Act
            var blocks = MarkdownToNotionConverter.ConvertToNotionBlocks(markdown);

            // Assert
            Assert.NotNull(blocks);
            Assert.NotEmpty(blocks);

            // Verify overall structure
            // Assert.Equal(12, blocks.Count); // 1 heading, 1 paragraph, 6 sections (each with heading and content)

            // Verify first heading (title)
            // var titleBlock = blocks[0] as HeadingBlock;
            // Assert.NotNull(titleBlock);
            // Assert.Equal("heading_1", titleBlock.Type);
            // Assert.Contains(titleBlock.Content.RichText, rt => GetPlainText(rt).Contains("NotionBae Roadmap"));
            //
            // // Verify first paragraph
            // var introBlock = blocks[1] as ParagraphBlock;
            // Assert.NotNull(introBlock);
            // Assert.Equal("paragraph", introBlock.Type);
            // Assert.Contains(introBlock.Paragraph.RichText, rt => GetPlainText(rt).Contains("planned features"));
            //
            // // Verify "Completed Features" section heading
            // var completedFeaturesHeading = blocks[2] as HeadingBlock;
            // Assert.NotNull(completedFeaturesHeading);
            // Assert.Equal("heading_2", completedFeaturesHeading.Type);
            // Assert.Contains(completedFeaturesHeading.Content.RichText, rt => GetPlainText(rt).Contains("Completed Features"));
            //
            // // Verify "Update Page Functionality" subsection
            // var updatePageHeading = blocks[3] as HeadingBlock;
            // Assert.NotNull(updatePageHeading);
            // Assert.Equal("heading_3", updatePageHeading.Type);
            // Assert.Contains(updatePageHeading.Content.RichText, rt => GetPlainText(rt).Contains("Update Page Functionality"));
            //
            // // Verify bulleted list items
            // var firstBulletPoint = blocks[4] as BulletedListBlock;
            // Assert.NotNull(firstBulletPoint);
            // Assert.Equal("bulleted_list_item", firstBulletPoint.Type);
            // string firstBulletText = GetPlainText(firstBulletPoint.BulletedListItem.RichText.First());
            // Assert.Contains("Description", firstBulletText);
            // Assert.Contains("update existing Notion pages", firstBulletText);
            //
            // // Check for the High Priority Items heading
            // var highPriorityHeading = blocks[7] as HeadingBlock;
            // Assert.NotNull(highPriorityHeading);
            // Assert.Equal("heading_2", highPriorityHeading.Type);
            // Assert.Contains(highPriorityHeading.Content.RichText, rt => GetPlainText(rt).Contains("High Priority Items"));

            // Verify nested list items under "Details"
            // Find a bullet point that contains "Details"
            var bulletWithDetails = blocks.OfType<BulletedListBlock>()
                .FirstOrDefault(b => b.BulletedListItem.RichText.Any(rt => GetPlainText(rt).Contains("Details")));
            
            Assert.NotNull(bulletWithDetails);
            Assert.NotEmpty(bulletWithDetails.BulletedListItem.Children);

            // Make sure we have indented bullet points
            var nestedBullet = bulletWithDetails.BulletedListItem.Children.FirstOrDefault() as BulletedListBlock;
            Assert.NotNull(nestedBullet);
            Assert.Contains(GetPlainText(nestedBullet.BulletedListItem.RichText), "indentation");
        }
        

        // Helper method to extract plain text from rich text objects
        [Fact]
        public void ConvertToNotionBlocks_ConvertTimelineAndStatus_ProducesCorrectBlockStructure()
        {
            // Arrange
            string markdown = @"**Target Timeline: Q3 2025**
        
        **Status: 🟡 In Progress**
        
        Updates have been made in branch: feature/3-sqlite-integration. A pull request will be submitted soon.";
        
            // Act
            var blocks = MarkdownToNotionConverter.ConvertToNotionBlocks(markdown);
        
            // Assert
            Assert.NotNull(blocks);
            Assert.NotEmpty(blocks);
        
            // Verify timeline block
            var timelineBlock = blocks[0] as ParagraphBlock;
            Assert.NotNull(timelineBlock);
            Assert.Contains(GetPlainText(timelineBlock.Paragraph.RichText), "Target Timeline: Q3 2025");
        
            // Verify status block
            // var statusBlock = blocks[1] as ParagraphBlock;
            // Assert.NotNull(statusBlock);
            // Assert.Contains(statusBlock.Paragraph.RichText, rt => GetPlainText(rt).Contains("Status: 🟡 In Progress"));
            //
            // // Verify update message
            // var updateBlock = blocks[2] as ParagraphBlock;
            // Assert.NotNull(updateBlock);
            // Assert.Contains(updateBlock.Paragraph.RichText, rt => GetPlainText(rt).Contains("Updates have been made"));
        }
        
        private string GetPlainText(object richText)
        {
            var propertyInfo = richText.GetType().GetProperty("plain_text");
            return propertyInfo?.GetValue(richText)?.ToString() ?? string.Empty;
        }
    }
}
