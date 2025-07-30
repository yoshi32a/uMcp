using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class AssetInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Guid { get; set; }
        public string Type { get; set; }

        [JsonPropertyName("size_bytes")] public long SizeBytes { get; set; }

        [JsonPropertyName("last_modified")] public string LastModified { get; set; }

        [JsonPropertyName("importer_type")] public string ImporterType { get; set; }

        public string[] Labels { get; set; }
        public string[] Dependencies { get; set; }

        [JsonPropertyName("dependency_count")] public int DependencyCount { get; set; }
    }
}