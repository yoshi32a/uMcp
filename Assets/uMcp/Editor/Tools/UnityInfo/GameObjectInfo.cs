using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class GameObjectInfo
    {
        public string Name { get; set; }
        public bool Active { get; set; }
        public string Tag { get; set; }
        public string Layer { get; set; }
        
        [JsonPropertyName("component_count")]
        public int ComponentCount { get; set; }
    }
}