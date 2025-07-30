using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using Cysharp.Threading.Tasks;
using uMCP.Editor.Core.DependencyInjection;
using uMCP.Editor.Core.Protocol;
using UnityEditor;
using UnityEngine;

namespace uMCP.Editor.Core
{
    /// <summary>Unity向けMCPサーバーのHTTPリスナーとパイプライン管理を行うクラス</summary>
    public sealed class UMcpServer : IDisposable
    {
        readonly HttpListener httpListener = new();
        readonly UMcpSettings settings;
        CancellationTokenSource cancellationTokenSource;
        bool isRunning;
        SimpleServiceContainer serviceContainer;

        /// <summary>サーバーが実行中かどうかを示すフラグ</summary>
        public bool IsRunning => isRunning;

        /// <summary>サーバーのURL文字列</summary>
        public string ServerUrl => settings.ServerUrl;

        /// <summary>UMcpServerの新しいインスタンスを初期化します</summary>
        public UMcpServer(UMcpSettings settings = null)
        {
            this.settings = settings ?? UMcpSettings.instance;
        }

        /// <summary>サーバーリソースを解放します</summary>
        public void Dispose()
        {
            Stop();
            httpListener?.Close();
        }

        /// <summary>MCPサーバーを非同期で開始します</summary>
        public async UniTask StartAsync()
        {
            if (isRunning)
                return;

            try
            {
                cancellationTokenSource = new CancellationTokenSource();

                var serverUrl = $"http://{settings.ipAddress}:{settings.port}{settings.serverPath}";
                httpListener.Prefixes.Add(serverUrl);
                httpListener.Start();

                isRunning = true;

                Log($"uMCP Server started at {serverUrl}");

                await RunAsync(cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                LogError($"Failed to start uMCP server: {ex.Message}");
                isRunning = false;
                throw;
            }
        }

        /// <summary>MCPサーバーを停止します</summary>
        public void Stop()
        {
            if (!isRunning)
                return;

            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            // サービスコンテナを破棄
            if (serviceContainer != null)
            {
                try
                {
                    serviceContainer.Dispose();
                }
                catch (Exception ex)
                {
                    LogError($"Error disposing service container: {ex.Message}");
                }
                finally
                {
                    serviceContainer = null;
                }
            }

            isRunning = false;

            Log("uMCP Server stopped");
        }

        /// <summary>MCPサーバーを非同期で停止します</summary>
        public async UniTask StopAsync()
        {
            if (!isRunning)
                return;

            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            if (serviceContainer != null)
            {
                try
                {
                    serviceContainer.Dispose();
                }
                catch (Exception ex)
                {
                    LogError($"Error disposing service container: {ex.Message}");
                }
                finally
                {
                    serviceContainer = null;
                }
            }

            isRunning = false;

            Log("uMCP Server stopped (async)");
        }

        /// <summary>MCPサーバーの実行ループを開始します</summary>
        async UniTask RunAsync(CancellationToken token)
        {
            Pipe clientToServerPipe = new();
            Pipe serverToClientPipe = new();

            var builder = new ServiceCollectionBuilder();

            if (settings.enableDefaultTools)
            {
                // デフォルトツールをロード
                LoadDefaultTools(builder);
                Log("Default MCP tools loaded");
            }

            LoadCustomTools(builder);

            serviceContainer = builder.Build();

            // MCPサーバーを作成して登録
            var mcpServer = new SimpleMcpServer(
                clientToServerPipe.Reader.AsStream(),
                serverToClientPipe.Writer.AsStream(),
                serviceContainer
            );
            serviceContainer.AddSingleton<IMcpServer>(mcpServer);

            HandleHttpRequestAsync(clientToServerPipe, serverToClientPipe, token).Forget();

            await mcpServer.RunAsync(token);
        }

        /// <summary>デフォルトツールをサービスコレクションに読み込みます</summary>
        void LoadDefaultTools(ServiceCollectionBuilder builder)
        {
            // ビルトインツールの実装を登録
            builder.AddSingleton(new Tools.UnityInfoToolImplementation());
            builder.AddSingleton(new Tools.AssetManagementToolImplementation());
            builder.AddSingleton(new Tools.ConsoleLogToolImplementation());
            builder.AddSingleton(new Tools.TestRunnerToolImplementation());
        }

        /// <summary>カスタムツールをサービスコレクションに読み込みます</summary>
        void LoadCustomTools(ServiceCollectionBuilder builder)
        {
            var toolGuids = AssetDatabase.FindAssets("t:UMcpToolBuilder");
            foreach (var guid in toolGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tool = AssetDatabase.LoadAssetAtPath<UMcpToolBuilder>(path);

                if (tool != null && tool.IsEnabled)
                {
                    try
                    {
                        tool.Build(builder);
                        Log($"Custom tool loaded: {tool.ToolName}");
                    }
                    catch (Exception ex)
                    {
                        LogError($"Failed to load tool '{tool.ToolName}': {ex.Message}");
                    }
                }
            }
        }

        /// <summary>HTTPリクエストを処理しパイプライン間でデータを中継します</summary>
        async UniTask HandleHttpRequestAsync(Pipe clientToServerPipe, Pipe serverToClientPipe, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    HttpListenerContext context = null;
                    try
                    {
                        context = await httpListener.GetContextAsync();
                    }
                    catch (HttpListenerException) when (token.IsCancellationRequested)
                    {
                        // サーバー停止時の正常な終了
                        return;
                    }

                    ProcessHttpContextAsync(context, clientToServerPipe, serverToClientPipe, token).Forget();
                }
            }
            catch (ObjectDisposedException)
            {
                // サーバー停止時は無視
            }
            catch (Exception ex)
            {
                Debug.LogError($"[uMCP] HTTP listener error: {ex.Message}");
            }
        }

        /// <summary>個別のHTTPコンテキストを処理します</summary>
        async UniTask ProcessHttpContextAsync(HttpListenerContext context, Pipe clientToServerPipe, Pipe serverToClientPipe, CancellationToken token)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                if (settings.enableCors)
                {
                    response.Headers.Add("Access-Control-Allow-Origin", "*");
                    response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                    response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                }

                switch (request.HttpMethod)
                {
                    case "OPTIONS":
                        response.StatusCode = 200;
                        break;

                    case "POST":
                        await HandlePostRequest(request, response, clientToServerPipe, serverToClientPipe, token);
                        break;

                    case "GET":
                        await HandleGetRequest(response);
                        break;

                    default:
                        response.StatusCode = 405; // Method Not Allowed
                        response.ContentType = "application/json";
                        var errorJson = "{\"error\": \"Method not allowed\"}";
                        await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(errorJson), token);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[uMCP] Request processing error: {ex.Message}");
                try
                {
                    response.StatusCode = 500;
                    response.ContentType = "application/json";
                    var errorMessage = ex.Message.Replace("\"", "\\\"");
                    var errorJson = $"{{\"error\": \"Internal server error: {errorMessage}\"}}";
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(errorJson), token);
                }
                catch
                {
                    // レスポンスの送信も失敗した場合は無視
                }
            }
            finally
            {
                try
                {
                    response.Close();
                }
                catch
                {
                    // クローズエラーは無視
                }
            }
        }

        /// <summary>POSTリクエストを処理します</summary>
        async UniTask HandlePostRequest(HttpListenerRequest request, HttpListenerResponse response, Pipe clientToServerPipe, Pipe serverToClientPipe,
            CancellationToken token)
        {
            using var inputReader = new StreamReader(request.InputStream, Encoding.UTF8);
            var inputBody = await inputReader.ReadToEndAsync();

            if (settings.debugMode)
            {
                Log($"Received request: {inputBody}");
            }

            if (string.IsNullOrWhiteSpace(inputBody))
            {
                response.StatusCode = 400;
                response.ContentType = "application/json";
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("{\"error\": \"Empty request body\"}"), token);
                return;
            }

            JsonNode inputBodyJson;
            try
            {
                inputBodyJson = JsonNode.Parse(inputBody);
            }
            catch (Exception ex)
            {
                response.StatusCode = 400;
                response.ContentType = "application/json";
                var errorMessage = ex.Message.Replace("\"", "\\\"");
                var errorJson = $"{{\"error\": \"Invalid JSON: {errorMessage}\"}}";
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(errorJson), token);
                return;
            }

            // notifications/initialized は応答不要
            if (inputBodyJson?["method"]?.ToString() == "notifications/initialized")
            {
                response.StatusCode = 200;
                response.ContentType = "application/json";
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("{\"result\": \"ok\"}"), token);
                return;
            }

            // MCPサーバーにリクエストを転送
            await clientToServerPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes(inputBody + "\n"), token);
            await clientToServerPipe.Writer.FlushAsync(token);

            // レスポンスを読み取り（タイムアウト付き）
            ReadResult result;
            using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(settings.timeoutSeconds));
                try
                {
                    result = await serverToClientPipe.Reader.ReadAsync(timeoutCts.Token);
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    response.StatusCode = 504; // Gateway Timeout
                    response.ContentType = "application/json";
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("{\"error\": \"Request timeout\"}"), token);
                    return;
                }
            }

            var buffer = result.Buffer;

            if (buffer.Length == 0)
            {
                response.StatusCode = 504; // Gateway Timeout
                response.ContentType = "application/json";
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("{\"error\": \"No response from MCP server\"}"), token);
                serverToClientPipe.Reader.AdvanceTo(buffer.End);
                return;
            }

            var resultBody = Encoding.UTF8.GetString(buffer.ToArray());
            serverToClientPipe.Reader.AdvanceTo(buffer.End);

            if (settings.debugMode)
            {
                Log($"Sending response: {resultBody}");
            }

            response.StatusCode = 200;
            response.ContentType = "application/json";
            await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(resultBody), token);
        }

        /// <summary>GETリクエストを処理します（サーバーステータス）</summary>
        async UniTask HandleGetRequest(HttpListenerResponse response)
        {
            var statusJson = @"{
    ""jsonrpc"": ""2.0"",
    ""result"": {
        ""status"": ""running"",
        ""server"": ""uMCP for Unity"",
        ""version"": """ + UMcpSettings.Version + @""",
        ""unity_version"": """ + Application.unityVersion + @""",
        ""platform"": """ + Application.platform.ToString() + @"""
    },
    ""id"": null
}";
            response.StatusCode = 200;
            response.ContentType = "application/json";
            await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(statusJson));
        }

        /// <summary>設定に応じてサーバーログを出力します</summary>
        void Log(string message)
        {
            if (settings.showServerLog)
            {
                Debug.Log($"[uMCP] {message}");
            }
        }

        /// <summary>エラーログを出力します</summary>
        void LogError(string message)
        {
            Debug.LogError($"[uMCP] {message}");
        }
    }
}
