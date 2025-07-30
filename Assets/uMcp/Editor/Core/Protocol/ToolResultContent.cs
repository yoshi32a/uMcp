using System.Text.Json.Serialization;

namespace uMCP.Editor.Core.Protocol
{
    /// <summary>ツール結果コンテンツ</summary>
    public class ToolResultContent
    {
        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("text")] public string Text { get; set; }
    }
}