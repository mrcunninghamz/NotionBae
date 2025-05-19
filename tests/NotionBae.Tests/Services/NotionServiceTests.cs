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

Use 3 dashes for an em-dash. Use 2 dashes for ranges (ex., ""it's all
in chapters 12--14""). Three dots ... will be converted to an ellipsis.
Unicode is supported. ☺



An h2 header
------------

Here's a numbered list:

 1. first item
 2. second item
 3. third item

Note again how the actual text starts at 4 columns in (4 characters
from the left side). Here's a code sample:

    # Let me re-iterate ...
    for i in 1 .. 10 { do-something(i) }

As you probably guessed, indented 4 spaces. By the way, instead of
indenting the block, you can use delimited blocks, if you like:

~~~
define foobar() {
    print ""Welcome to flavor country!"";
}
~~~

(which makes copying & pasting easier). You can optionally mark the
delimited block for Pandoc to syntax highlight it:

~~~python
import time
# Quick, count to ten!
for i in range(10):
    # (but not *too* quick)
    time.sleep(0.5)
    print i
~~~



### An h3 header ###

Now a nested list:

 1. First, get these ingredients:

      * carrots
      * celery
      * lentils

 2. Boil some water.

 3. Dump everything in the pot and follow
    this algorithm:

        find wooden spoon
        uncover pot
        stir
        cover pot
        balance wooden spoon precariously on pot handle
        wait 10 minutes
        goto first step (or shut off burner when done)

    Do not bump wooden spoon or it will fall.

Notice again how text always lines up on 4-space indents (including
that last line which continues item 3 above).

Here's a link to [a website](http://foo.com)


[^1]: Footnote text goes here.

Tables can look like this:

| Language | Primary Use Case | First Release | Typing |
|----------|-----------------|---------------|---------|
| Python | Data Science | 1991 | Dynamic |
| Rust | Systems Programming | 2010 | Static |
| JavaScript | Web Development | 1995 | Dynamic |
| Go | Cloud Infrastructure | 2009 | Static |
| Kotlin | Android Development | 2011 | Static |




And note that you can backslash-escape any punctuation characters
which you wish to be displayed literally, ex.: \`foo\`, \*bar\*, etc.
";
        
        // Act
        var result = await TestSubject.CreatePage("Test", "Test", content);
    }
    
    [Fact]
    public async Task AppendBlockChildren_WithValidPageContent_SendsProperRequest()
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

Use 3 dashes for an em-dash. Use 2 dashes for ranges (ex., ""it's all
in chapters 12--14""). Three dots ... will be converted to an ellipsis.
Unicode is supported. ☺



An h2 header
------------

Here's a numbered list:

 1. first item
 2. second item
 3. third item

Note again how the actual text starts at 4 columns in (4 characters
from the left side). Here's a code sample:

    # Let me re-iterate ...
    for i in 1 .. 10 { do-something(i) }

As you probably guessed, indented 4 spaces. By the way, instead of
indenting the block, you can use delimited blocks, if you like:

~~~
define foobar() {
    print ""Welcome to flavor country!"";
}
~~~

(which makes copying & pasting easier). You can optionally mark the
delimited block for Pandoc to syntax highlight it:

~~~python
import time
# Quick, count to ten!
for i in range(10):
    # (but not *too* quick)
    time.sleep(0.5)
    print i
~~~



### An h3 header ###

Now a nested list:

 1. First, get these ingredients:

      * carrots
      * celery
      * lentils

 2. Boil some water.

 3. Dump everything in the pot and follow
    this algorithm:

        find wooden spoon
        uncover pot
        stir
        cover pot
        balance wooden spoon precariously on pot handle
        wait 10 minutes
        goto first step (or shut off burner when done)

    Do not bump wooden spoon or it will fall.

Notice again how text always lines up on 4-space indents (including
that last line which continues item 3 above).

Here's a link to [a website](http://foo.com)


[^1]: Footnote text goes here.

Tables can look like this:

| Language | Primary Use Case | First Release | Typing |
|----------|-----------------|---------------|---------|
| Python | Data Science | 1991 | Dynamic |
| Rust | Systems Programming | 2010 | Static |
| JavaScript | Web Development | 1995 | Dynamic |
| Go | Cloud Infrastructure | 2009 | Static |
| Kotlin | Android Development | 2011 | Static |




And note that you can backslash-escape any punctuation characters
which you wish to be displayed literally, ex.: \`foo\`, \*bar\*, etc.
";
        
        // Act
        var result = await TestSubject.AppendBlockChildren("Test", content);
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

    [Fact]
    public async Task NoOrphanedBullets()
    {
        // arrange
        var content = @"
## Create Multiple Inventory Repositories per App

### TODO List
- Inventory core PR 🟢 Completed
- Let DevOps know of the new API pipeline with Azure Function template (appId: 39312, primary/secondary regions) 🟢 Completed
- Inventory API PR 🟢 Completed
- Create enabler for DevOps to update APIM endpoints:
  - INT: Point APIM to new front door location for integration environment ⌚ In progress
  - CERT: Point APIM to new front door location for certification environment 🔵 Not started
  - PROD: Point APIM to new front door location for production environment 🔵 Not started

";
        
        // act
        var appendBlocks = TestSubject.MarkdownToNotionAppend(content);
    }
}
