using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class EditorInfo
    {
        [JsonPropertyName("is_batch_mode")]
        public bool IsBatchMode { get; set; }
        
        [JsonPropertyName("build_guid")]
        public string BuildGuid { get; set; }
        
        [JsonPropertyName("cloud_project_id")]
        public string CloudProjectId { get; set; }
    }
}