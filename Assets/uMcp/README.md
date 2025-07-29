# Unity MCP Server

A powerful Model Context Protocol (MCP) server implementation for Unity Editor that enables AI assistants to interact with Unity projects in real-time.

## Features

### 🚀 Core MCP Server
- **HTTP Server**: Runs on `localhost:49001/umcp/` by default
- **JSON-RPC 2.0**: Full MCP protocol compliance
- **Real-time Communication**: Direct integration with Unity Editor
- **Auto-start**: Automatically starts when Unity Editor loads

### 🛠️ Built-in Tools

#### Unity Information Tool
- `get_unity_info` - Get Unity editor and project details
- `get_scene_info` - Analyze current scene structure
- `log_message` - Output messages to Unity console

#### Asset Management Tool
- `refresh_assets` - Refresh Unity asset database
- `save_project` - Save current project and assets
- `find_assets` - Search assets by filter and folder
- `get_asset_info` - Get detailed asset information
- `reimport_asset` - Force reimport specific assets

#### Console Log Tool
- `get_console_logs` - Retrieve Unity console logs with filtering
- `clear_console_logs` - Clear all console logs
- `log_to_console` - Output custom messages to console
- `get_log_statistics` - Get console log statistics

#### Test Runner Tool
- `run_edit_mode_tests` - Execute EditMode tests with timeout control
- `run_play_mode_tests` - Execute PlayMode tests with domain reload control
- `get_available_tests` - List available tests by mode (EditMode/PlayMode/All)

## Installation

### Method 1: Package Manager (Git URL)
1. Open Unity Package Manager
2. Click "+" → "Add package from git URL"
3. Enter: `https://github.com/your-username/unity-mcp-server.git`

### Method 2: Manual Installation
1. Download the latest release
2. Extract to `Packages/com.umcp.unity-mcp-server/`
3. Unity will automatically detect and import the package

## Quick Start

1. **Auto-start Server**: Server starts automatically when Unity loads
2. **Manual Control**: Use `Tools > uMCP` menu for server management
3. **Create Tool Assets**: Run `Tools > uMCP > Create Default Tool Assets`
4. **Test Connection**: Connect MCP Inspector to `http://localhost:49001/umcp/`

## Configuration

Access settings via `Tools > uMCP > Open Settings`:

- **Server Address**: Default `127.0.0.1:49001`
- **Server Path**: Default `/umcp/`
- **Auto Start**: Enable/disable automatic server startup
- **Debug Mode**: Enable detailed request/response logging
- **CORS Support**: Enable for web-based MCP clients

## MCP Client Integration

### MCP Inspector
1. Open [MCP Inspector](https://inspector.mcp.run/)
2. Set Transport to `HTTP`
3. Enter URL: `http://localhost:49001/umcp/`
4. Click Connect

### Claude Code
Configure in your MCP client settings:
```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "curl",
      "args": ["-X", "POST", "http://localhost:49001/umcp/"]
    }
  }
}
```

## Development

### Creating Custom Tools

1. Create a new ScriptableObject that inherits from `UMcpToolBuilder`
2. Implement the `Build` method to register your tool services
3. Use `[McpServerToolType]` and `[McpServerTool]` attributes

Example:
```csharp
[CreateAssetMenu(fileName = "MyCustomTool", menuName = "uMCP/Tools/My Custom Tool")]
public class MyCustomTool : UMcpToolBuilder
{
    public override void Build(IServiceCollection services)
    {
        services.AddSingleton<MyCustomToolImplementation>();
    }
}

[McpServerToolType, Description("My custom tool")]
internal sealed class MyCustomToolImplementation
{
    [McpServerTool, Description("Do something custom")]
    public async ValueTask<object> DoSomething()
    {
        await UniTask.SwitchToMainThread();
        return new { success = true, message = "Custom action completed" };
    }
}
```

## Advanced Features

### PlayMode Test Execution with Domain Reload Control

The Unity MCP Server includes advanced PlayMode test execution with domain reload optimization:

**Key Features:**
- **Domain Reload Control**: Automatically disables domain reload during PlayMode tests for faster execution
- **High Performance**: PlayMode tests execute in ~0.25 seconds instead of minutes
- **Automatic Settings Restoration**: EditorSettings are safely restored after test completion
- **HTTP Compatible**: Fast execution makes PlayMode tests viable through MCP HTTP requests

**Parameters:**
- `disableDomainReload` (default: `true`) - Controls domain reload behavior during PlayMode tests
- `timeoutSeconds` - Configurable timeout for test execution
- Assembly and category filtering support

**Performance Comparison:**
- Traditional PlayMode tests: 60+ seconds with domain reload
- Optimized PlayMode tests: ~0.25 seconds without domain reload

### EditMode Test Execution

- Standard Unity Test Framework integration
- Configurable timeout and filtering options
- Consistent ~0.6 seconds execution time for typical test suites

## Requirements

- **Unity**: 6000.0 or later
- **UniTask**: 2.3.3 or later (automatically installed)
- **.NET**: Compatible with Unity's .NET implementation

## Troubleshooting

### Common Issues

**Server won't start:**
- Check if port 49001 is available
- Try changing port in settings
- Check Unity console for error messages

**Tools not appearing:**
- Ensure tool assets are created (`Tools > uMCP > Create Default Tool Assets`)
- Restart the server (`Tools > uMCP > Restart Server`)
- Check assembly compilation errors

**MCP Inspector connection fails:**
- Verify server is running (`Tools > uMCP > Show Server Info`)
- Check firewall settings
- Ensure CORS is enabled if using web clients

**PlayMode tests timeout or fail:**
- Ensure `disableDomainReload` is set to `true` (default)
- Check if Unity is currently in Play Mode (tests cannot run during Play Mode)
- Verify Unity Test Framework is properly configured
- For manual testing, use Unity Test Runner window as fallback

**EditMode tests not executing:**
- Check for compilation errors in Unity console
- Ensure test assemblies are properly configured
- Verify test methods follow Unity Test Framework conventions

## Architecture

```
Unity MCP Server
├── Core/
│   ├── UMcpServer.cs          # HTTP server implementation
│   ├── UMcpServerManager.cs   # Unity integration & lifecycle
│   └── UMcpToolBuilder.cs     # Tool registration base class
├── Settings/
│   └── UMcpSettings.cs        # Configuration management
└── Tools/
    ├── UnityInfoTool.cs       # Unity information tools
    ├── AssetManagementTool.cs # Asset operations
    ├── ConsoleLogTool.cs      # Console log management
    └── TestRunnerTool.cs      # Test execution tools
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Acknowledgments

- Inspired by [Unity Natural MCP](https://github.com/notargs/UnityNaturalMCP)
- Built with [UniTask](https://github.com/Cysharp/UniTask)
- Implements [Model Context Protocol](https://github.com/modelcontextprotocol)