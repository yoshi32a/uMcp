using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class PrefabDetailResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("asset_path")]
        public string AssetPath { get; set; }
        
        [JsonPropertyName("tag")]
        public string Tag { get; set; }
        
        [JsonPropertyName("layer")]
        public string Layer { get; set; }
        
        [JsonPropertyName("position")]
        public Vector3Info Position { get; set; }
        
        [JsonPropertyName("rotation")]
        public Vector3Info Rotation { get; set; }
        
        [JsonPropertyName("scale")]
        public Vector3Info Scale { get; set; }
        
        [JsonPropertyName("components")]
        public ComponentInfo[] Components { get; set; }
        
        [JsonPropertyName("child_count")]
        public int ChildCount { get; set; }
        
        [JsonPropertyName("children")]
        public string[] Children { get; set; }
    }
}