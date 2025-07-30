using System.Text.Json.Serialization;

namespace uMCP.Editor.Core.Protocol
{
    /// <summary>サーバー能力</summary>
    public class ServerCapabilities
    {
        [JsonPropertyName("tools")] public object Tools { get; set; } = new { };
    }
}