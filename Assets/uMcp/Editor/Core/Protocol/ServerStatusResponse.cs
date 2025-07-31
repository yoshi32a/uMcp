using System.Text.Json.Serialization;

namespace uMCP.Editor.Core.Protocol
{
    /// <summary>サーバーステータスレスポンス</summary>
    public class ServerStatusResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("server")]
        public string Server { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("unity_version")]
        public string UnityVersion { get; set; }

        [JsonPropertyName("platform")]
        public string Platform { get; set; }
    }
}