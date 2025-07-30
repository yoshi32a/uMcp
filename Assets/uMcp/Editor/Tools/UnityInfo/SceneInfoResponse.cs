using System.Text.Json.Serialization;

namespace uMCP.Editor.Tools
{
    public class SceneInfoResponse
    {
        [JsonPropertyName("scene_name")] public string SceneName { get; set; }

        [JsonPropertyName("scene_path")] public string ScenePath { get; set; }

        [JsonPropertyName("scene_build_index")]
        public int SceneBuildIndex { get; set; }

        [JsonPropertyName("is_loaded")] public bool IsLoaded { get; set; }

        [JsonPropertyName("is_dirty")] public bool IsDirty { get; set; }

        [JsonPropertyName("gameobject_count")] public int GameObjectCount { get; set; }

        [JsonPropertyName("root_gameobjects")] public GameObjectInfo[] RootGameObjects { get; set; }
    }
}
