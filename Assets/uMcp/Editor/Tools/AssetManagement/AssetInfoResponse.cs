using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class AssetInfoResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        
        [JsonPropertyName("asset_info")]
        public AssetInfo AssetInfo { get; set; }
    }
}