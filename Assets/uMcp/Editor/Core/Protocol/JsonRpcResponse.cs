using System.Text.Json.Serialization;

namespace uMCP.Editor.Core.Protocol
{
    /// <summary>JSON-RPC 2.0 レスポンス</summary>
    public class JsonRpcResponse
    {
        [JsonPropertyName("jsonrpc")] public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("result")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object Result { get; set; }

        [JsonPropertyName("error")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonRpcError Error { get; set; }

        [JsonPropertyName("id")] public object Id { get; set; }
    }
}