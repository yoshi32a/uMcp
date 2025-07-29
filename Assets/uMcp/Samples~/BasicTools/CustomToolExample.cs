using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using uMCP.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace uMCP.Editor.Samples
{
    /// <summary>カスタムMCPツールの作成例</summary>
    [CreateAssetMenu(fileName = "CustomToolExample", menuName = "uMCP/Tools/Custom Tool Example")]
    public class CustomToolExample : UMcpToolBuilder
    {
        void OnEnable()
        {
            toolName = "Custom Tool Example";
            description = "Example of creating custom MCP tools";
        }

        public override void Build(IServiceCollection services)
        {
            services.AddSingleton<CustomToolImplementation>();
        }
    }

    /// <summary>カスタムツールの実装例</summary>
    [McpServerToolType, Description("カスタムツールの実装例")]
    internal sealed class CustomToolImplementation
    {
        /// <summary>簡単な挨拶メッセージを返す</summary>
        [McpServerTool, Description("カスタム挨拶メッセージを返す")]
        public async ValueTask<object> SayCustomHello(
            [Description("挨拶する相手の名前")] string name = "World")
        {
            await UniTask.SwitchToMainThread();

            return new
            {
                message = $"Hello {name} from Custom MCP Tool!",
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                custom_data = new
                {
                    tool_name = "CustomToolExample",
                    unity_version = Application.unityVersion,
                    editor_platform = Application.platform.ToString()
                }
            };
        }

        /// <summary>Unity エディターの状態情報を取得</summary>
        [McpServerTool, Description("Unity エディターの現在の状態を取得")]
        public async ValueTask<object> GetEditorState()
        {
            await UniTask.SwitchToMainThread();

            return new
            {
                is_playing = Application.isPlaying,
                is_focused = Application.isFocused,
                is_paused = EditorApplication.isPaused,
                is_compiling = EditorApplication.isCompiling,
                is_updating = EditorApplication.isUpdating,
                current_scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                build_target = EditorUserBuildSettings.activeBuildTarget.ToString(),
                development_build = EditorUserBuildSettings.development
            };
        }

        /// <summary>指定した数の警告ログを出力</summary>
        [McpServerTool, Description("指定した数の警告ログを Unity コンソールに出力")]
        public async ValueTask<object> GenerateWarningLogs(
            [Description("出力する警告ログの数")] int count = 3,
            [Description("ログのプレフィックス")] string prefix = "Custom Tool")
        {
            await UniTask.SwitchToMainThread();

            if (count < 1 || count > 10)
            {
                return new
                {
                    success = false,
                    error = "Count must be between 1 and 10"
                };
            }

            for (int i = 1; i <= count; i++)
            {
                Debug.LogWarning($"[{prefix}] Warning message #{i} - This is a test warning from custom MCP tool");
            }

            return new
            {
                success = true,
                message = $"Generated {count} warning logs with prefix '{prefix}'",
                logs_generated = count,
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        /// <summary>指定したGameObjectの情報を取得</summary>
        [McpServerTool, Description("指定した名前のGameObjectの情報を取得")]
        public async ValueTask<object> GetGameObjectInfo(
            [Description("情報を取得するGameObjectの名前")] string gameObjectName)
        {
            await UniTask.SwitchToMainThread();

            if (string.IsNullOrEmpty(gameObjectName))
            {
                return new
                {
                    success = false,
                    error = "GameObject name is required"
                };
            }

            var gameObject = GameObject.Find(gameObjectName);
            if (gameObject == null)
            {
                return new
                {
                    success = false,
                    error = $"GameObject '{gameObjectName}' not found in current scene"
                };
            }

            var components = gameObject.GetComponents<Component>();

            return new
            {
                success = true,
                gameobject_info = new
                {
                    name = gameObject.name,
                    active = gameObject.activeSelf,
                    active_in_hierarchy = gameObject.activeInHierarchy,
                    tag = gameObject.tag,
                    layer = LayerMask.LayerToName(gameObject.layer),
                    transform = new
                    {
                        position = gameObject.transform.position,
                        rotation = gameObject.transform.rotation.eulerAngles,
                        scale = gameObject.transform.localScale
                    },
                    component_count = components.Length,
                    components = System.Array.ConvertAll(components, c => c.GetType().Name),
                    child_count = gameObject.transform.childCount,
                    has_parent = gameObject.transform.parent != null
                }
            };
        }
    }
}