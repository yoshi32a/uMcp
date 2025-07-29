# Unity MCP Server

A powerful Model Context Protocol (MCP) server implementation for Unity Editor that enables AI assistants to interact with Unity projects in real-time.

## Overview

Unity MCP Server provides a complete MCP server implementation that runs inside Unity Editor, allowing AI assistants like Claude to directly interact with your Unity projects. It includes built-in tools for asset management, console logs, test execution, and project analysis.

## Documentation

- [English Documentation](Assets/uMcp/README.md)
- [日本語ドキュメント](README_ja.md)

## Quick Start

### Prerequisites

1. **Unity 6000.0** or later
2. **UniTask** - Install via Package Manager: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
3. **NuGetForUnity** - Install via Package Manager: `https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity`
4. **ModelContextProtocol** - Install via NuGet after NuGetForUnity is installed

### Installation

```
https://github.com/yoshi32a/uMcp.git?path=Assets/uMcp
```

Add the above URL in Unity Package Manager.

## Features

- 🚀 **HTTP Server** running on `localhost:49001/umcp/`
- 🛠️ **18 Built-in Tools** for Unity interaction
- ⚡ **Optimized PlayMode Tests** with domain reload control
- 📦 **Easy Integration** with auto-start functionality
- 🔧 **Extensible Framework** for custom tools

## License

MIT License - see [LICENSE.md](LICENSE.md) for details.

## Repository Structure

```
UnityMcpTest/
├── Assets/
│   ├── uMcp/              # Unity MCP Server package
│   │   ├── Editor/        # Core implementation
│   │   ├── package.json   # Package manifest
│   │   └── README.md      # Full documentation
│   └── packages.config    # NuGet packages
├── README.md              # This file
├── README_ja.md           # Japanese documentation
└── CLAUDE.md              # AI assistant instructions
```