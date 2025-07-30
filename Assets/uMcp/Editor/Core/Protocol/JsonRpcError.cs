using System.Text.Json.Serialization;

namespace uMCP.Editor.Core.Protocol
{
    /// <summary>JSON-RPC 2.0 エラー</summary>
    public class JsonRpcError
    {
        [JsonPropertyName("code")] public int Code { get; set; }

        [JsonPropertyName("message")] public string Message { get; set; }

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object Data { get; set; }
    }
}