using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace uMCP.Editor.Core.Protocol
{
    /// <summary>MCP 初期化リクエスト</summary>
    public class InitializeRequest
    {
        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; }

        [JsonPropertyName("capabilities")]
        public ClientCapabilities Capabilities { get; set; }

        [JsonPropertyName("clientInfo")]
        public ClientInfo ClientInfo { get; set; }
    }

    /// <summary>クライアント能力</summary>
    public class ClientCapabilities
    {
        [JsonPropertyName("tools")]
        public ToolsCapability Tools { get; set; }
    }

    /// <summary>ツール能力</summary>
    public class ToolsCapability
    {
        [JsonPropertyName("callTool")]
        public object CallTool { get; set; }
    }

    /// <summary>クライアント情報</summary>
    public class ClientInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }
    }

    /// <summary>MCP 初期化レスポンス</summary>
    public class InitializeResult
    {
        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; }

        [JsonPropertyName("capabilities")]
        public ServerCapabilities Capabilities { get; set; }

        [JsonPropertyName("serverInfo")]
        public ServerInfo ServerInfo { get; set; }
    }

    /// <summary>サーバー能力</summary>
    public class ServerCapabilities
    {
        [JsonPropertyName("tools")]
        public object Tools { get; set; } = new { };
    }

    /// <summary>サーバー情報</summary>
    public class ServerInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }
    }

    /// <summary>ツール一覧リクエスト</summary>
    public class ListToolsRequest
    {
    }

    /// <summary>ツール一覧レスポンス</summary>
    public class ListToolsResult
    {
        [JsonPropertyName("tools")]
        public List<ToolInfo> Tools { get; set; }
    }

    /// <summary>ツール情報</summary>
    public class ToolInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("inputSchema")]
        public Dictionary<string, object> InputSchema { get; set; }
    }

    /// <summary>ツール呼び出しリクエスト</summary>
    public class CallToolRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("arguments")]
        public Dictionary<string, object> Arguments { get; set; }
    }

    /// <summary>ツール呼び出しレスポンス</summary>
    public class CallToolResult
    {
        [JsonPropertyName("content")]
        public List<ToolResultContent> Content { get; set; }

        [JsonPropertyName("isError")]
        public bool IsError { get; set; }
    }

    /// <summary>ツール結果コンテンツ</summary>
    public class ToolResultContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}