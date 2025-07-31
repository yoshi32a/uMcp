using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class HierarchyAnalysisResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("object_type")]
        public string ObjectType { get; set; } = "hierarchy_analysis";
        
        [JsonPropertyName("analysis_timestamp")]
        public string AnalysisTimestamp { get; set; }
        
        [JsonPropertyName("unity_version")]
        public string UnityVersion { get; set; }
        
        [JsonPropertyName("root_object")]
        public HierarchyNode RootObject { get; set; }
        
        [JsonPropertyName("analysis")]
        public HierarchyAnalysis Analysis { get; set; }
    }

    public class HierarchyNode
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("active")]
        public bool Active { get; set; }
        
        [JsonPropertyName("tag")]
        public string Tag { get; set; }
        
        [JsonPropertyName("layer")]
        public string Layer { get; set; }
        
        [JsonPropertyName("component_count")]
        public int ComponentCount { get; set; }
        
        [JsonPropertyName("key_components")]
        public string[] KeyComponents { get; set; }
        
        [JsonPropertyName("issues")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[] Issues { get; set; }
        
        [JsonPropertyName("children")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public HierarchyNode[] Children { get; set; }
    }

    public class HierarchyAnalysis
    {
        [JsonPropertyName("total_objects")]
        public int TotalObjects { get; set; }
        
        [JsonPropertyName("max_depth")]
        public int MaxDepth { get; set; }
        
        [JsonPropertyName("ui_elements")]
        public UIAnalysis UIElements { get; set; }
        
        [JsonPropertyName("performance_concerns")]
        public string[] PerformanceConcerns { get; set; }
        
        [JsonPropertyName("design_issues")]
        public string[] DesignIssues { get; set; }
        
        [JsonPropertyName("missing_references")]
        public string[] MissingReferences { get; set; }
        
        [JsonPropertyName("recommendations")]
        public string[] Recommendations { get; set; }
    }

    public class UIAnalysis
    {
        [JsonPropertyName("canvas_count")]
        public int CanvasCount { get; set; }
        
        [JsonPropertyName("button_count")]
        public int ButtonCount { get; set; }
        
        [JsonPropertyName("text_count")]
        public int TextCount { get; set; }
        
        [JsonPropertyName("image_count")]
        public int ImageCount { get; set; }
        
        [JsonPropertyName("input_count")]
        public int InputCount { get; set; }
        
        [JsonPropertyName("custom_ui_count")]
        public int CustomUICount { get; set; }
        
        [JsonPropertyName("layout_groups")]
        public int LayoutGroups { get; set; }
        
        [JsonPropertyName("ui_structure")]
        public string UIStructure { get; set; }
    }
}