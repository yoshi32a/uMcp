using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    /// <summary>全MCPツール共通の標準レスポンス形式</summary>
    public class StandardResponse
    {
        public bool Success { get; set; }
        
        [JsonPropertyName("formatted_output")]
        public string FormattedOutput { get; set; }
        
        public string Error { get; set; }
        public string Message { get; set; }
    }
}