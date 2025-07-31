using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using uMCP.Editor.Core.Attributes;
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
                    ComponentCount = go.GetComponents<Component>().Length
                })
            };
        }
    }
}
