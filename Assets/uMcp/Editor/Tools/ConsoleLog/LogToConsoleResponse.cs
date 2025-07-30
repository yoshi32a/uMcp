using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class LogToConsoleResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        [JsonPropertyName("full_message")] public string FullMessage { get; set; }

        public string Timestamp { get; set; }
    }
}
