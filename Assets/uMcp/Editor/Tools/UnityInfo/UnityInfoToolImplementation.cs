using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;

namespace uMCP.Editor.Tools
{
    /// <summary>Unity情報ツールの実装</summary>
    [McpServerToolType, Description("Unity情報を取得するツール")]
    internal sealed class UnityInfoToolImplementation
    {

        /// <summary>Unity エディターとプロジェクトの情報を取得</summary>
        [McpServerTool, Description("Unity エディターとプロジェクトの詳細情報を取得")]
        public async ValueTask<object> GetUnityInfo()
        {
            await UniTask.SwitchToMainThread();

            return new UnityInfoResponse
            {
                UnityVersion = Application.unityVersion,
                Platform = Application.platform.ToString(),
                ProjectName = Application.productName,
                CompanyName = Application.companyName,
                DataPath = Application.dataPath,
                PersistentDataPath = Application.persistentDataPath,
                StreamingAssetsPath = Application.streamingAssetsPath,
                IsPlaying = Application.isPlaying,
                IsFocused = Application.isFocused,
                SystemLanguage = Application.systemLanguage.ToString(),
                EditorInfo = new EditorInfo
                {
                    IsBatchMode = Application.isBatchMode,
                    BuildGuid = Application.buildGUID,
                    CloudProjectId = Application.cloudProjectId
                }
            };
        }

        /// <summary>現在のシーン情報を取得</summary>
        [McpServerTool, Description("現在のシーン情報とGameObjectの一覧を取得")]
        public async ValueTask<object> GetSceneInfo()
        {
            await UniTask.SwitchToMainThread();

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var gameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            return new SceneInfoResponse
            {
                SceneName = scene.name,
                ScenePath = scene.path,
                SceneBuildIndex = scene.buildIndex,
                IsLoaded = scene.isLoaded,
                IsDirty = scene.isDirty,
                GameObjectCount = gameObjects.Length,
                RootGameObjects = System.Array.ConvertAll(scene.GetRootGameObjects(), go => new GameObjectInfo
                {
                    Name = go.name,
                    Active = go.activeSelf,
                    Tag = go.tag,
                    Layer = LayerMask.LayerToName(go.layer),
                    ComponentCount = go.GetComponents<UnityEngine.Component>().Length
                })
            };
        }

        /// <summary>Unity コンソールにメッセージをログ出力</summary>
        [McpServerTool, Description("Unity コンソールにメッセージをログ出力")]
        public async ValueTask<object> LogMessage([Description("ログメッセージ")] string message = "Test message", [Description("ログタイプ: log, warning, error")] string logType = "log")
        {
            await UniTask.SwitchToMainThread();

            switch (logType.ToLower())
            {
                case "error":
                    Debug.LogError($"[MCP Tool] {message}");
                    break;
                case "warning":
                    Debug.LogWarning($"[MCP Tool] {message}");
                    break;
                default:
                    Debug.Log($"[MCP Tool] {message}");
                    break;
            }

            return new LogMessageResponse
            {
                Success = true,
                Message = $"Logged {logType}: {message}",
                Timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
    }
}
