using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class ComponentInfo
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("full_type")]
        public string FullType { get; set; }
        
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
    }
}