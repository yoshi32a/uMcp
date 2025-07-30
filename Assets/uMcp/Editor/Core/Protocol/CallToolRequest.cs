using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace uMCP.Editor.Core.Protocol
{
    /// <summary>ツール呼び出しリクエスト</summary>
    public class CallToolRequest
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("arguments")] public Dictionary<string, object> Arguments { get; set; }
    }
}