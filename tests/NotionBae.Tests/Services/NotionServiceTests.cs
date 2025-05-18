using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NotionBae.Services;
using NotionBae.Utilities;
using TestBae.BaseClasses.AutoFixture;
using Xunit;
using AutoFixture;

namespace NotionBae.Tests.Services;

public class NotionServiceTests : BaseTest<NotionService>
{
    private Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private HttpClient _httpClient;
    
    protected override void ConfigureFixture()
    {
        _mockHttpMessageHandler = Fixture.Freeze<Mock<HttpMessageHandler>>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.notion.com/v1/")
        };
        
        // Register the HttpClient in the Fixture to be used when creating the NotionService
        Fixture.Register(() => _httpClient);
        // Configure IConfiguration to return a fake API key
        Fixture.Freeze<Mock<IConfiguration>>()
            .Setup(c => c["NotionApiKey"])
            .Returns("fake-api-key");
    }

    [Fact]
    public async Task CreatePage_WithValidPageContent_SendsProperRequest()
    {
        // Arrange 
        var content = @"An h1 header
============

Paragraphs are separated by a blank line.

2nd paragraph. *Italic*, **bold**, and `monospace`. Itemized lists
look like:

  * this one
  * that one
  * the other one

Note that --- not considering the asterisk --- the actual text
content starts at 4-columns in.

> Block quotes are
> written like so.
>
> They can span multiple paragraphs,
> if you like.


";
        
        // Act
        var result = await TestSubject.CreatePage("Test", "Test", content);
    }

    [Fact]
    public async Task UpdateBlock_WithMarkdownContent_SendsProperRequest()
    {
        // Arrange
        var blockId = "test-block-id";
        var markdownContent = "**Target Timeline: Q3 2025**\n\n**Status: 🟡 In Progress**\n\nUpdates have been made in branch: feature/3-sqlite-integration. A pull request will be submitted soon.";
        
        var expectedBlocks = MarkdownToNotionConverter.ConvertToNotionBlocks(markdownContent);
        var expectedPayload = new { children = expectedBlocks };
        
        var expectedJsonContent = JsonSerializer.Serialize(expectedPayload);

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"success\": true}")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Patch && 
                    req.RequestUri.ToString().Contains($"blocks/{blockId}")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

        // Act
        var result = await TestSubject.UpdateBlock(blockId, markdownContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        
        _mockHttpMessageHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch &&
                    req.RequestUri.ToString() == $"https://api.notion.com/v1/blocks/{blockId}"),
                ItExpr.IsAny<CancellationToken>()
            );
    }
}
