# Unity MCP Server

A powerful Model Context Protocol (MCP) server implementation for Unity Editor that enables AI assistants to interact with Unity projects in real-time.

## Features

### 🚀 Core MCP Server
- **HTTP + JSON-RPC 2.0** protocol on `127.0.0.1:49001/umcp/`
- **Auto-start** with Unity Editor
- **Real-time** Unity project interaction
- **Custom tool** framework with ScriptableObject integration

### 🛠️ Built-in Tools (17 total)

| Category | Tools | Key Features |
|----------|-------|-------------|
| **Unity Info** | `get_unity_info`, `get_scene_info` | Editor details, scene analysis |
| **Asset Management** | `refresh_assets`, `save_project`, `find_assets`, `get_asset_info`, `reimport_asset` | Complete asset lifecycle |
| **Console Logs** | `get_console_logs`, `clear_console_logs`, `log_to_console`, `get_log_statistics` | Log management & analytics |
| **Test Runner** | `run_edit_mode_tests`, `run_play_mode_tests`, `get_available_tests` | Optimized test execution |

## Installation

### Prerequisites
Install these dependencies via Unity Package Manager (`+` → `Add package from git URL`):

1. **UniTask**: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
2. **NuGetForUnity**: `https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity`
3. **System.Text.Json**: Search in `NuGet → Manage NuGet Packages` → Install v9.0.7+

### Install Unity MCP Server

**Via Package Manager (Recommended):**
```
https://github.com/yoshi32a/uMcp.git?path=Assets/uMcp
```

**Alternative methods:**
- **Manual**: Download from [Releases](https://github.com/yoshi32a/uMcp/releases) → Extract to `Assets/` or `Packages/`
- **UnityPackage**: Download `.unitypackage` from [Releases](https://github.com/yoshi32a/uMcp/releases) → Import via Unity

## Quick Start

1. **Auto-start Server**: Server starts automatically when Unity loads
2. **Manual Control**: Use `Tools > uMCP` menu for server management
3. **Create Tool Assets**: Run `Tools > uMCP > Create Default Tool Assets`
4. **Test Connection**: Connect MCP Inspector to `http://127.0.0.1:49001/umcp/`

## Usage

### Configuration
Access settings via `Tools > uMCP > Open Settings`:
- **Server**: `127.0.0.1:49001/umcp/` (default)
- **Auto Start**: Automatic server startup
- **Debug Mode**: Request/response logging
- **CORS**: Web client support

### Connect MCP Clients

| Client | Connection |
|--------|------------|
| **MCP Inspector** | [inspector.mcp.run](https://inspector.mcp.run/) → HTTP → `http://127.0.0.1:49001/umcp/` |
| **Claude CLI** | `claude mcp add -s project --transport http unity-mcp-server http://127.0.0.1:49001/umcp/` |
| **Custom Client** | HTTP POST to `http://127.0.0.1:49001/umcp/` with JSON-RPC 2.0 |

## Development

### Custom Tools
Create ScriptableObject-based tools with attribute-driven registration:

```csharp
[McpServerToolType, Description("My custom tool")]
internal sealed class MyCustomToolImplementation
{
    [McpServerTool, Description("Do something custom")]
    public async ValueTask<object> DoSomething([Description("Input parameter")] string input = "default")
    {
        await UniTask.SwitchToMainThread(); // Required for Unity API access
        return new { success = true, message = $"Processed: {input}" };
    }
}
```

**Key Points:**
- Use `await UniTask.SwitchToMainThread()` before Unity API calls
- Add `[Description]` for better MCP client integration
- Return serializable objects (records, anonymous types, POCOs)

## Requirements

- **Unity 6000.0+** (Unity 6 or later)
- **UniTask 2.3.3+** (async processing)
- **System.Text.Json 9.0.7+** (JSON serialization)

## License

MIT License - see [LICENSE](LICENSE) for details.

## Support

- **Issues**: [GitHub Issues](https://github.com/yoshi32a/uMcp/issues)
- **Reference**: [Unity Natural MCP](https://github.com/johniwasz/unity-natural-mcp)