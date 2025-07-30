using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace uMCP.Editor.Core.Protocol
{
    /// <summary>JSON-RPC 2.0 リクエスト</summary>
    public class JsonRpcRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("method")]
        public string Method { get; set; }

        [JsonPropertyName("params")]
        public Dictionary<string, object> Params { get; set; }

        [JsonPropertyName("id")]
        public object Id { get; set; }
    }

    /// <summary>JSON-RPC 2.0 レスポンス</summary>
    public class JsonRpcResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("result")]
        public object Result { get; set; }

        [JsonPropertyName("error")]
        public JsonRpcError Error { get; set; }

        [JsonPropertyName("id")]
        public object Id { get; set; }
    }

    /// <summary>JSON-RPC 2.0 エラー</summary>
    public class JsonRpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public object Data { get; set; }
    }

    /// <summary>JSON-RPC エラーコード</summary>
    public static class JsonRpcErrorCodes
    {
        public const int ParseError = -32700;
        public const int InvalidRequest = -32600;
        public const int MethodNotFound = -32601;
        public const int InvalidParams = -32602;
        public const int InternalError = -32603;
    }
}