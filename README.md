# NotionBae

A Model Context Protocol (MCP) Server for Notion.com integration, built with .NET C# using [ModelContextProtocol](https://github.com/modelcontextprotocol). NotionBae leverages the [Notion API](https://developers.notion.com/) to provide seamless interaction between AI assistants and Notion workspaces.

## Overview

NotionBae provides a bridge between Large Language Models and Notion.com through the Model Context Protocol framework. This allows AI assistants to interact with Notion databases, pages, and content in a seamless and structured way.

## Features

- Integration with Notion API
- MCP-compliant server implementation
- Structured data handling for Notion content
- Authentication and secure access to Notion workspaces

## Technologies

- **[Notion SDK for .NET](https://github.com/notion-dotnet/notion-sdk-net)**: A comprehensive .NET client for the Notion API. While we use this SDK as a foundation, we've implemented our own `NotionBaeRestClient` to address several limitations:
  - The original SDK's REST client implementation was tightly coupled and didn't follow .NET best practices for dependency injection
  - Our custom client properly utilizes `HttpClientFactory`, which is crucial for proper handling of connection pooling and lifetime management
  - This redesign allows for better testability and enables polymorphic behavior in our test suites
  - The solution provides true dependency injection rather than the pseudo-DI implementation in the original SDK
- **[Markdig](https://github.com/xoofx/markdig)**: A fast, powerful, and extensible Markdown processor for .NET used for converting between Notion's block structure and Markdown

### Why Markdown?

NotionBae uses Markdown as an intermediary format between Notion's complex block structure and AI agents for several reasons:

1. **Token Efficiency**: Notion's API returns data in a verbose block structure with many properties. By converting to Markdown, we significantly reduce the number of tokens processed by AI models, potentially improving performance and reducing costs.

2. **Readability**: Markdown is inherently human-readable, making the content more accessible for both humans and AI agents.

3. **Simplicity**: Working with Markdown is much simpler than handling Notion's nested block structure directly, especially when generating content.

While there's no comprehensive data comparing token usage between raw Notion API responses and Markdown, anecdotal evidence suggests notable efficiency gains when using Markdown as an intermediary format.

## Available Tools

NotionBae provides the following tools for interacting with Notion:

| Tool Name | Description | Parameters |
|-----------|-------------|------------|
| nb_search | Searches Notion pages and returns their titles and public URLs | `query`: String - The search query to find matching pages in Notion |
| nb_create_page | Creates a new Notion page with the given title, description, and content | `parentId`: String - ID of the parent page<br>`title`: String - Title of the new page<br>`description`: String - Description for the page<br>`content`: String - Markdown content for the page |
| nb_get_page_content | Retrieves a Notion page with its metadata and full content in markdown format | `pageId`: String - ID of the page to retrieve |
| nb_get_block | Retrieves a specific block and its children from Notion | `blockId`: String - ID of the block to retrieve |
| nb_search_page | Advanced page search with filtering capabilities | `query`: String - The search query<br>`filter`: Object - Optional filter parameters |
| nb_update_page | Updates an existing Notion page | `pageId`: String - ID of the page to update<br>`content`: String - New markdown content for the page |

## Getting Started

### 1. Notion Account Setup

1. **Create a Notion Account**: If you don't already have one, sign up for a Notion account at [notion.so](https://www.notion.so/).
   
2. **Create a Notion Integration**:
   - Go to [Notion Developers](https://www.notion.so/my-integrations)
   - Click "New integration"
   - Give your integration a name (e.g., "NotionBae")
   - Select the workspace where you want to use the integration
   - Set appropriate capabilities (at minimum, select "Read content" and "Update content")
   - Click "Submit" to create your integration

3. **Get your API Key**:
   - After creating the integration, you'll see your "Internal Integration Secret" (API key)
   - Copy this key as you'll need it for configuring NotionBae

4. **Share Pages with your Integration**:
   - Open a Notion page you want to access through NotionBae
   - Click "Share" in the top right
   - In the "Add people, groups, or integrations" field, search for your integration name
   - Select your integration to grant it access to the page

### 2. Clone and Setup NotionBae

1. **Clone the repository**:
   ```bash
   git clone https://github.com/mrcunninghamz/NotionBae.git /path/to/your/directory
   cd /path/to/your/directory
   ```
   
   Replace `/path/to/your/directory` with the actual directory path where you want to clone the repository (e.g., `~/projects/NotionBae` or `/Users/username/Documents/Projects/NotionBae`).

2. **Build the project**:
   ```bash
   dotnet build
   ```

3. **Set up Environment Variables**:

   The NotionBae server requires the following environment variables:
   
   - `Notion:AuthToken`: Your Notion API key for authentication with Notion's API 
   
   You can obtain a Notion API key by creating an integration in your Notion workspace settings. Visit the [Notion API documentation](https://developers.notion.com/docs/getting-started) for more information on creating an integration and obtaining an API key. 
   Copy appsettings.template.json to appsettings.json and fill in the value for `Notion:AuthToken` with your Notion API key.


4. **Run the project**:
   ```bash
   dotnet run --project PATH_TO_NOTIONBAE_PROJECT/src/NotionBae.csproj
   ```
   Replace `PATH_TO_NOTIONBAE_PROJECT` with the path to your NotionBae project folder (the folder containing the NotionBae.csproj file).

### 3. MCP Server Configuration

To use NotionBae as an MCP server in VS Code, add the following configuration to your VS Code `settings.json` file:

```json
"mcp": {
  "servers": {
      "NotionBae": {
        "url": "http://localhost:5000/mcp"
      }
  }
}
```

## Related Projects

This project is part of the "Bae" collection of tools:

- [TestBae](https://github.com/mrcunninghamz/TestBae) - A .NET testing utility library that simplifies test setup with fixtures, mocks, and common testing patterns for more efficient and maintainable test suites.
- [AzBae](https://github.com/mrcunninghamz/AzBae) - Azure DevOps tooling that provides both CLI and GUI interfaces to streamline Azure resource management, deployments, and DevOps workflows.

## License

