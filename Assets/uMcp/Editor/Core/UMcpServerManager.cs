using Cysharp.Threading.Tasks;
using UnityEditor;

namespace uMCP.Editor.Core
{
    /// <summary>Unity Editor向けMCPサーバーの管理とメニュー操作を提供するクラス</summary>
    [InitializeOnLoad]
    public static class UMcpServerManager
    {
        static UMcpServer server;
        static bool initialized;

        /// <summary>現在のMCPサーバーインスタンス</summary>
        public static UMcpServer Server => server;

        /// <summary>サーバーが実行中かどうかを示すフラグ</summary>
        public static bool IsRunning => server?.IsRunning ?? false;

        /// <summary>静的コンストラクタでサーバーマネージャーを初期化します</summary>
        static UMcpServerManager()
        {
            Initialize();
        }

        /// <summary>サーバーマネージャーを初期化しイベントハンドラーを登録します</summary>
        static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            server = new UMcpServer();

            // アセンブリリロード前の処理
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.quitting += OnQuit;

            if (UMcpSettings.instance.autoStart)
            {
                StartServer();
            }

            initialized = true;
        }

        /// <summary>MCPサーバーを開始します</summary>
        [MenuItem("uMCP/Start Server")]
        public static void StartServer()
        {
            if (server == null)
            {
                server = new UMcpServer();
            }

            server.StartAsync().Forget();
        }

        /// <summary>MCPサーバーを停止します</summary>
        [MenuItem("uMCP/Stop Server")]
        public static void StopServer()
        {
            server?.StopAsync().Forget();
        }

        /// <summary>MCPサーバーを再起動します</summary>
        [MenuItem("uMCP/Restart Server")]
        public static void RestartServer()
        {
            RestartServerAsync().Forget();
        }

        /// <summary>MCPサーバーを非同期で再起動します</summary>
        static async UniTask RestartServerAsync()
        {
            if (server != null)
            {
                await server.StopAsync();
            }

            StartServer();
        }

        /// <summary>uMCP設定ウィンドウを開きます</summary>
        [MenuItem("uMCP/Open Settings")]
        public static void OpenSettings()
        {
            var settings = UMcpSettings.instance;
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        /// <summary>サーバー情報を表示します</summary>
        [MenuItem("uMCP/Show Server Info")]
        public static void ShowServerInfo()
        {
            var settings = UMcpSettings.instance;
            var status = IsRunning ? "Running" : "Stopped";
            var message = $"uMCP Server Status: {status}\n" +
                          $"URL: {settings.ServerUrl}\n" +
                          $"Version: {UMcpSettings.Version}\n" +
                          $"Auto Start: {settings.autoStart}\n" +
                          $"Default Tools: {settings.enableDefaultTools}\n" +
                          $"Debug Mode: {settings.debugMode}";

            EditorUtility.DisplayDialog("uMCP Server Info", message, "OK");
        }

        /// <summary>プレイモード変更時の処理を行います</summary>
        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            // プレイモード変更時の処理
        }

        /// <summary>アセンブリリロード前にサーバーリソースを適切に解放します</summary>
        static void OnBeforeAssemblyReload()
        {
            if (server != null)
            {
                // アセンブリリロード時は同期処理で強制停止
                server.Stop();
                server.Dispose();
                server = null;
            }

            initialized = false;
        }

        /// <summary>Unity終了時にサーバーリソースを解放します</summary>
        static void OnQuit()
        {
            server?.Dispose();
        }
    }
}
