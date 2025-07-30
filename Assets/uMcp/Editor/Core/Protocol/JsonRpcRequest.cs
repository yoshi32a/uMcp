using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace uMCP.Editor.Core.Protocol
{
    /// <summary>JSON-RPC 2.0 リクエスト</summary>
    public class JsonRpcRequest
    {
        [JsonPropertyName("jsonrpc")] public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("method")] public string Method { get; set; }

        [JsonPropertyName("params")] public Dictionary<string, object> Params { get; set; }

        [JsonPropertyName("id")] public object Id { get; set; }
    }
}
