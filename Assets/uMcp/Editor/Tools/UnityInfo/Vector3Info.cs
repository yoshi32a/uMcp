using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class Vector3Info
    {
        [JsonPropertyName("x")]
        public float X { get; set; }
        
        [JsonPropertyName("y")]
        public float Y { get; set; }
        
        [JsonPropertyName("z")]
        public float Z { get; set; }
    }
}