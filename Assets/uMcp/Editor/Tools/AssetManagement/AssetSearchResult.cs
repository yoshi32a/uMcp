using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class AssetSearchResult
    {
        public string Guid { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }

        [JsonPropertyName("size_bytes")] public long SizeBytes { get; set; }

        [JsonPropertyName("last_modified")] public string LastModified { get; set; }
    }
}