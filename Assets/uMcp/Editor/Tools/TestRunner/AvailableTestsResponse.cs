using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class AvailableTestsResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }

        [JsonPropertyName("requested_mode")]
        public string RequestedMode { get; set; }

        public List<TestModeInfo> Tests { get; set; }
        public string Note { get; set; }
        public string Timestamp { get; set; }
    }
}
