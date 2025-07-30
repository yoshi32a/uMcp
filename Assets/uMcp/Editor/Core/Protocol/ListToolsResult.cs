using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace uMCP.Editor.Core.Protocol
{
    /// <summary>ツール一覧レスポンス</summary>
    public class ListToolsResult
    {
        [JsonPropertyName("tools")] public List<ToolInfo> Tools { get; set; }
    }
}