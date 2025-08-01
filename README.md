# Unity MCP Server

A powerful Model Context Protocol (MCP) server implementation for Unity Editor that enables AI assistants to interact with Unity projects in real-time with intelligent workflow guidance.

## Overview

Unity MCP Server provides a complete MCP server implementation that runs inside Unity Editor, allowing AI assistants like Claude to directly interact with your Unity projects. Features **21 built-in tools**, **Markdown-based workflow system**, and **intelligent action suggestions** for asset management, console logs, test execution, project analysis, and context-aware development workflows.

## Documentation

- [English Documentation](Assets/uMcp/README.md)
- [日本語ドキュメント](README_ja.md)

## Quick Start

### Prerequisites

1. **Unity 2022.3 LTS** or later
2. **UniTask** - Install via Package Manager: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`

### Installation

```
https://github.com/yoshi32a/uMcp.git?path=Assets/uMcp
```

Add the above URL in Unity Package Manager.

## Features

- 🚀 **HTTP Server** running on `localhost:49001/umcp/`
- 🛠️ **21 Built-in Tools** across 6 categories for comprehensive Unity interaction
- 🧠 **Markdown Workflow System** with intelligent action suggestions
- ⚡ **Optimized PlayMode Tests** with domain reload control
- 🔍 **Enhanced Error Detection** with corrected console log filtering
- 📦 **Easy Integration** with auto-start functionality
- 🔧 **Extensible Framework** for custom tools

### New in Latest Version
- **🆕 Workflow Guidance**: Context-aware next-action suggestions
- **📝 Markdown Workflows**: Easy-to-edit workflow definitions
- **🎯 Smart Triggers**: Automatic tool chaining based on context
- **🐛 Bug Fixes**: Resolved `get_console_logs` errorsOnly filtering issue

## License

MIT License - see [LICENSE.md](LICENSE.md) for details.

## Tool Categories

### 🎯 Unity Information (5 tools)
- Project analysis, scene inspection, GameObject details

### 📁 Asset Management (5 tools)  
- Asset search, refresh, import management

### 🐛 Console Logs (4 tools)
- Log retrieval, filtering, statistics (with fixed errorsOnly bug)

### 🧪 Test Execution (3 tools)
- EditMode/PlayMode test running with domain reload optimization

### ⚙️ Editor Extensions (1 tool)
- Custom method execution for development automation

### 🧠 Workflow Guidance (2 tools) **NEW!**
- Intelligent next-action suggestions and Markdown workflow patterns

## Repository Structure

```
UnityMcpTest/
├── Assets/
│   ├── uMcp/              # Unity MCP Server package
│   │   ├── Editor/        # Core implementation (21 tools)
│   │   ├── Workflows/     # Markdown workflow definitions
│   │   ├── package.json   # Package manifest
│   │   └── README.md      # Full documentation
│   └── packages.config    # NuGet packages
├── README.md              # This file (updated)
├── README_ja.md           # Japanese documentation
└── CLAUDE.md              # AI assistant instructions (updated)
```