# Changelog

All notable changes to Unity MCP Server will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-07-30

### Added
- Initial release of Unity MCP Server
- Core MCP server implementation with HTTP transport
- Auto-start functionality integrated with Unity Editor
- Comprehensive tool system with 4 built-in tool categories

#### Core Features
- **HTTP Server**: Runs on configurable port (default: 49001)
- **JSON-RPC 2.0**: Full Model Context Protocol compliance
- **Real-time Communication**: Direct Unity Editor integration
- **Error Handling**: Robust error handling with timeout support
- **CORS Support**: Configurable CORS for web-based clients

#### Built-in Tools
- **Unity Information Tool**
  - `get_unity_info`: Unity editor and project information
  - `get_scene_info`: Current scene structure analysis
  - `log_message`: Console logging with different levels

- **Asset Management Tool**
  - `refresh_assets`: Asset database refresh
  - `save_project`: Project and asset saving
  - `find_assets`: Asset search with filtering
  - `get_asset_info`: Detailed asset information
  - `reimport_asset`: Force asset reimport

- **Console Log Tool**
  - `get_console_logs`: Retrieve console logs with filtering
  - `clear_console_logs`: Clear all console logs
  - `log_to_console`: Custom console output
  - `get_log_statistics`: Console log statistics

- **Test Runner Tool**
  - `run_edit_mode_tests`: Execute EditMode tests
  - `run_play_mode_tests`: Execute PlayMode tests
  - `get_available_tests`: List available tests

#### Configuration & Management
- **Unity Menu Integration**: `Tools > uMCP` menu
- **Settings Management**: ProjectSettings integration
- **Tool Asset Creation**: Automated tool asset management
- **Debug Mode**: Detailed request/response logging

#### Developer Experience
- **Custom Tool Framework**: Extensible tool creation system
- **ScriptableObject Integration**: Unity-native tool configuration
- **Automatic Discovery**: Dynamic tool loading
- **Assembly Reload Handling**: Proper cleanup during code changes

### Technical Details
- **Unity Version**: Requires Unity 6000.0 or later
- **Dependencies**: UniTask 2.3.3+, ModelContextProtocol 0.3.0+
- **Architecture**: Modular design with clear separation of concerns
- **Performance**: Optimized for Unity Editor performance
- **Security**: Local-only server with configurable access

### Documentation
- Comprehensive README with quick start guide
- API documentation for all tools
- Architecture overview
- Troubleshooting guide
- Custom tool development examples

## [Unreleased]

### Planned Features
- GameObject manipulation tools
- Build automation tools  
- Package Manager integration
- Performance profiling tools
- Scene management utilities
- Custom inspector integrations

---

## Development Notes

This project was inspired by [Unity Natural MCP](https://github.com/notargs/UnityNaturalMCP) and aims to provide a comprehensive, production-ready MCP server implementation for Unity development workflows.

### Architecture Decisions
- **HTTP over Stdio**: Chosen for better debugging and client compatibility
- **ScriptableObject Tools**: Unity-native approach for tool configuration
- **UniTask Integration**: Async/await support optimized for Unity
- **Modular Design**: Easy to extend and maintain

### Performance Considerations
- **Main Thread Switching**: All Unity API calls properly marshaled
- **Resource Management**: Proper disposal of server resources
- **Assembly Reload**: Graceful handling of code recompilation
- **Error Recovery**: Robust error handling without editor crashes