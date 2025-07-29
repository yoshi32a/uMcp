using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class AssetSearchResponse
    {
        public bool Success { get; set; }
        
        [JsonPropertyName("search_filter")]
        public string SearchFilter { get; set; }
        
        [JsonPropertyName("search_folder")]
        public string SearchFolder { get; set; }
        
        [JsonPropertyName("total_found")]
        public int TotalFound { get; set; }
        
        [JsonPropertyName("returned_count")]
        public int ReturnedCount { get; set; }
        
        public AssetSearchResult[] Results { get; set; }
    }
}