using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class AssetOperationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        [JsonPropertyName("duration_ms")] public double DurationMs { get; set; }

        public string Timestamp { get; set; }
    }
}
