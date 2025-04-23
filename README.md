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
| Search    | Searches Notion pages and returns their titles and public URLs | `query`: String - The search query to find matching pages in Notion |

## Getting Started

### MCP Server Configuration

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
        "PATH_TO_NOTIONBAE_PROJECT/NotionBae.csproj"
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
  "C:\\Projects\\NotionBae\\NotionBae.csproj"
]
```

### Environment Variables

The NotionBae server requires the following environment variables:

- `NotionApiKey`: Your Notion API key for authentication with Notion's API

You can obtain a Notion API key by creating an integration in your Notion workspace settings. Visit the [Notion API documentation](https://developers.notion.com/docs/getting-started) for more information on creating an integration and obtaining an API key.

### Running the Server

The MCP server is implemented in the `Program.cs` file and can be run directly using:

```
dotnet run
```

from the project directory.

## Related Projects

This project is part of the "Bae" collection of tools:
- TestBae - Testing utilities
- AzBae - Azure tooling

## License

More information coming soon.