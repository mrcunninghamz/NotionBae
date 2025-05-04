# NotionBae

A Model Context Protocol (MCP) Server for Notion.com integration, built with .NET C# using [ModelContextProtocol](https://github.com/modelcontextprotocol). NotionBae leverages the [Notion API](https://developers.notion.com/) to provide seamless interaction between AI assistants and Notion workspaces.

## Overview

NotionBae provides a bridge between Large Language Models and Notion.com through the Model Context Protocol framework. This allows AI assistants to interact with Notion databases, pages, and content in a seamless and structured way.

## Features

- Integration with Notion API
- MCP-compliant server implementation
- Structured data handling for Notion content
- Authentication and secure access to Notion workspaces

## Available Tools

NotionBae provides the following tools for interacting with Notion:

| Tool Name | Description | Parameters |
|-----------|-------------|------------|
| nb_search | Searches Notion pages and returns their titles and public URLs | `query`: String - The search query to find matching pages in Notion |
| nb_create_page | Creates a new Notion page with the given title, description, and content | `parentId`: String - ID of the parent page<br>`title`: String - Title of the new page<br>`description`: String - Description for the page<br>`content`: String - Markdown content for the page |
| nb_get_page_content | Retrieves a Notion page with its metadata and full content in markdown format | `pageId`: String - ID of the page to retrieve |

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

### 3. MCP Server Configuration

To use NotionBae as an MCP server in VS Code, add the following configuration to your VS Code `settings.json` file:

```json
"mcp": {
  "inputs": [
    {
      "type": "promptString",
      "id": "notion_api_key",
      "description": "Notion API Key",
      "password": true
    }
  ],
  "servers": {
    "NotionBae": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "PATH_TO_NOTIONBAE_PROJECT\\src\\NotionBae.csproj"
      ],
      "env": {
        "NotionApiKey": "${input:notion_api_key}"
      }
    }
  }
}
```

Replace `PATH_TO_NOTIONBAE_PROJECT` with the path to your NotionBae project folder (the folder containing the NotionBae.csproj file).

For example, if your NotionBae project is located at `C:\Projects\NotionBae\`, the configuration would be:

```json
"args": [
  "run",
  "--project",
  "C:\\Projects\\NotionBae\\src\\NotionBae.csproj"
]
```

### Environment Variables

The NotionBae server requires the following environment variables:

- `NotionApiKey`: Your Notion API key for authentication with Notion's API

You can obtain a Notion API key by creating an integration in your Notion workspace settings. Visit the [Notion API documentation](https://developers.notion.com/docs/getting-started) for more information on creating an integration and obtaining an API key.

### Running the Server

There are several ways to start the NotionBae MCP server:

1. **Using VS Code Commands**:
   - Open the Command Palette (`Cmd+Shift+P` on Mac or `Ctrl+Shift+P` on Windows/Linux)
   - Type "MCP: Start Server" and select it from the dropdown
   - Choose "NotionBae" from the list of available servers

2. **Using Context Menu**:
   - In your VS Code settings.json file, hover over the server name ("NotionBae")
   - Click on the "Start Server" button that appears above the server configuration
   - VS Code will prompt you for your Notion API key (as configured in the inputs section)

3. **Direct Command Line** (alternative method):
   - You can also run the server directly using the command line from the project directory:
   ```
   dotnet run
   ```
   - Note: When using this method, you'll need to ensure the NotionApiKey environment variable is set separately

Once the server is running, it will be available for use with compatible AI assistants that support the Model Context Protocol.

## MCP Inspector

NotionBae is compatible with the Model Context Protocol (MCP) Inspector tool, which helps you validate and debug your MCP server implementation.

### What is MCP Inspector?

The MCP Inspector is a command-line utility that allows you to:
- Validate that your MCP server conforms to the MCP specification
- Test your server's responses to various requests
- Debug issues with tool execution and response handling
- Explore the capabilities and schema of your server

### Running the Inspector with NotionBae

To run the MCP Inspector with NotionBae, navigate to your NotionBae project directory and execute:

```bash
npx @modelcontextprotocol/inspector dotnet run
```

This command will:
1. Start your NotionBae MCP server using `dotnet run`
2. Launch the MCP Inspector, which will connect to your running server
3. Validate your server implementation against the MCP specification
4. Provide a detailed report of any issues or recommendations

The inspector will help ensure that NotionBae is properly implementing the MCP protocol and that all tools are correctly defined and functioning.

## Related Projects

This project is part of the "Bae" collection of tools:

- [TestBae](https://github.com/mrcunninghamz/TestBae) - A .NET testing utility library that simplifies test setup with fixtures, mocks, and common testing patterns for more efficient and maintainable test suites.
- [AzBae](https://github.com/mrcunninghamz/AzBae) - Azure DevOps tooling that provides both CLI and GUI interfaces to streamline Azure resource management, deployments, and DevOps workflows.

## License