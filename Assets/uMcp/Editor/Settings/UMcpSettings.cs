using UnityEngine;
using UnityEditor;

namespace uMCP.Editor
{
    [FilePath("ProjectSettings/uMcpSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class UMcpSettings : ScriptableSingleton<UMcpSettings>
    {
        public const string Version = "1.0.9";

        [Header("Server Configuration")] [Tooltip("The IP address to bind the server to. Use 127.0.0.1 for local access only.")]
        public string ipAddress = "127.0.0.1";

        [Tooltip("The port number for the MCP server. Default is 49001 (dynamic port range).")]
        public int port = 49001;

        [Tooltip("The server path prefix. Must start and end with /.")]
        public string serverPath = "/umcp/";

        [Header("Features")] [Tooltip("Show server logs in the Unity console.")]
        public bool showServerLog = false;

        [Tooltip("Load built-in MCP tools automatically.")]
        public bool enableDefaultTools = true;

        [Tooltip("Start the MCP server automatically when Unity starts.")]
        public bool autoStart = true;

        [Header("Advanced")] [Tooltip("Maximum number of concurrent connections.")]
        public int maxConnections = 10;

        [Tooltip("Request timeout in seconds.")]
        public int timeoutSeconds = 300;

        [Header("Client Support")] [Tooltip("Enable CORS support for web-based clients.")]
        public bool enableCors = true;

        [Tooltip("Log detailed request/response data for debugging.")]
        public bool debugMode;

        public string ServerUrl => $"http://{ipAddress}:{port}{serverPath}";

        public void Save()
        {
            Save(true);
        }

        void OnValidate()
        {
            // サーバーパスの検証
            if (!string.IsNullOrEmpty(serverPath))
            {
                if (!serverPath.StartsWith("/"))
                    serverPath = "/" + serverPath;
                if (!serverPath.EndsWith("/"))
                    serverPath = serverPath + "/";
            }

            // ポート番号の範囲チェック
            port = Mathf.Clamp(port, 1024, 65535);

            // タイムアウトの範囲チェック
            timeoutSeconds = Mathf.Max(1, timeoutSeconds);
            maxConnections = Mathf.Max(1, maxConnections);
        }
    }
}
