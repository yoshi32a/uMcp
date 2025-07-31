using System;
using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class GameObjectAnalysisResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("object_type")]
        public string ObjectType { get; set; } = "gameObject";
        
        [JsonPropertyName("analysis_timestamp")]
        public string AnalysisTimestamp { get; set; }
        
        [JsonPropertyName("unity_version")]
        public string UnityVersion { get; set; }
        
        [JsonPropertyName("summary")]
        public GameObjectSummary Summary { get; set; }
        
        [JsonPropertyName("transform")]
        public TransformInfo Transform { get; set; }
        
        [JsonPropertyName("components")]
        public EnhancedComponentInfo[] Components { get; set; }
        
        [JsonPropertyName("hierarchy")]
        public HierarchyInfo Hierarchy { get; set; }
    }

    public class GameObjectSummary
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("component_count")]
        public int ComponentCount { get; set; }
        
        [JsonPropertyName("child_count")]
        public int ChildCount { get; set; }
        
        [JsonPropertyName("active")]
        public bool Active { get; set; }
        
        [JsonPropertyName("active_in_hierarchy")]
        public bool ActiveInHierarchy { get; set; }
        
        [JsonPropertyName("tag")]
        public string Tag { get; set; }
        
        [JsonPropertyName("layer")]
        public string Layer { get; set; }
    }

    public class TransformInfo
    {
        [JsonPropertyName("position")]
        public Vector3Info Position { get; set; }
        
        [JsonPropertyName("rotation")]
        public Vector3Info Rotation { get; set; }
        
        [JsonPropertyName("scale")]
        public Vector3Info Scale { get; set; }
        
        [JsonPropertyName("world_position")]
        public Vector3Info WorldPosition { get; set; }
        
        [JsonPropertyName("world_rotation")]
        public Vector3Info WorldRotation { get; set; }
        
        [JsonPropertyName("world_scale")]
        public Vector3Info WorldScale { get; set; }
    }

    public class EnhancedComponentInfo
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("full_type")]
        public string FullType { get; set; }
        
        [JsonPropertyName("category")]
        public string Category { get; set; }
        
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    public class HierarchyInfo
    {
        [JsonPropertyName("full_path")]
        public string FullPath { get; set; }
        
        [JsonPropertyName("depth")]
        public int Depth { get; set; }
        
        [JsonPropertyName("parent")]
        public string Parent { get; set; }
        
        [JsonPropertyName("children")]
        public string[] Children { get; set; }
        
        [JsonPropertyName("sibling_index")]
        public int SiblingIndex { get; set; }
    }
}
