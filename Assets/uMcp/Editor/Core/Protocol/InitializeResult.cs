using System.Text.Json.Serialization;

namespace uMCP.Editor.Core.Protocol
{
    /// <summary>MCP 初期化レスポンス</summary>
    public class InitializeResult
    {
        [JsonPropertyName("protocolVersion")] public string ProtocolVersion { get; set; }

        [JsonPropertyName("capabilities")] public ServerCapabilities Capabilities { get; set; }

        [JsonPropertyName("serverInfo")] public ServerInfo ServerInfo { get; set; }
    }
}
