using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class UnityInfoResponse
    {
        [JsonPropertyName("unity_version")] public string UnityVersion { get; set; }

        public string Platform { get; set; }

        [JsonPropertyName("project_name")] public string ProjectName { get; set; }

        [JsonPropertyName("company_name")] public string CompanyName { get; set; }

        [JsonPropertyName("data_path")] public string DataPath { get; set; }

        [JsonPropertyName("persistent_data_path")]
        public string PersistentDataPath { get; set; }

        [JsonPropertyName("streaming_assets_path")]
        public string StreamingAssetsPath { get; set; }

        [JsonPropertyName("is_playing")] public bool IsPlaying { get; set; }

        [JsonPropertyName("is_focused")] public bool IsFocused { get; set; }

        [JsonPropertyName("system_language")] public string SystemLanguage { get; set; }

        [JsonPropertyName("editor_info")] public EditorInfo EditorInfo { get; set; }
    }
}
