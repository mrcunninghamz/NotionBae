using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NotionBae.Services;
using NotionBae.Utilities;
using Xunit;

namespace NotionBae.Tests.Services;

public class NotionServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<NotionService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly NotionService _notionService;

    public NotionServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.notion.com/v1/")
        };
        _mockLogger = new Mock<ILogger<NotionService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        
        _mockConfiguration.Setup(c => c["NotionApiKey"]).Returns("fake-api-key");
        
        _notionService = new NotionService(_httpClient, _mockConfiguration.Object, _mockLogger.Object);
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
        var result = await _notionService.UpdateBlock(blockId, markdownContent);

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
