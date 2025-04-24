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
| nb_get_page | Retrieves a Notion page by its ID | `pageId`: String - ID of the page to retrieve |
| nb_retrieve_block_children | Retrieves children blocks of a specified block or page ID and converts them to markdown. In other words, it gets the content of a Notion page or block and converts it to markdown | `blockId`: String - ID of the block or page to retrieve content from |