using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace uMCP.Editor.Core.Protocol
{
    /// <summary>ツール情報</summary>
    public class ToolInfo
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("inputSchema")] public Dictionary<string, object> InputSchema { get; set; }
    }
}