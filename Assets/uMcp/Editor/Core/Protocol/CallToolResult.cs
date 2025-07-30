using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace uMCP.Editor.Core.Protocol
{
    /// <summary>ツール呼び出しレスポンス</summary>
    public class CallToolResult
    {
        [JsonPropertyName("content")] public List<ToolResultContent> Content { get; set; }

        [JsonPropertyName("isError")] public bool IsError { get; set; }
    }
}