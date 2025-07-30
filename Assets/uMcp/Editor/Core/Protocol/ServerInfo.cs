using System.Text.Json.Serialization;

namespace uMCP.Editor.Core.Protocol
{
    /// <summary>サーバー情報</summary>
    public class ServerInfo
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("version")] public string Version { get; set; }
    }
}