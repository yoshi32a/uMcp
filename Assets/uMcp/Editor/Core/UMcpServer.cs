using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        public UniTask StopAsync()
        {
            if (!isRunning)
                return UniTask.CompletedTask;

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
            return UniTask.CompletedTask;
        }

        /// <summary>MCPサーバーの実行ループを開始します</summary>
        async UniTask RunAsync(CancellationToken token)
        {
            var builder = new ServiceCollectionBuilder();

            if (settings.enableDefaultTools)
            {
                // デフォルトツールをロード
                LoadDefaultTools(builder);
                Log("Default MCP tools loaded");
            }

            LoadCustomTools(builder);

            serviceContainer = builder.Build();

            // 直接HTTP処理モードでサーバー実行
            await HandleHttpRequestDirectAsync(token);
        }

        /// <summary>直接HTTP処理でMCPリクエストを処理します</summary>
        async UniTask HandleHttpRequestDirectAsync(CancellationToken token)
        {
            // SimpleMcpServerインスタンスを作成 - セッション管理付き
            var sessionManager = new SessionManager();
            
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
                        return;
                    }

                    ProcessMcpRequestAsync(context, sessionManager, token).Forget();
                }
            }
            catch (ObjectDisposedException)
            {
                // サーバー停止時は無視
            }
            catch (Exception ex)
            {
                LogError($"HTTP listener error: {ex.Message}");
            }
        }

        /// <summary>個別のMCPリクエストを処理します</summary>
        async UniTask ProcessMcpRequestAsync(HttpListenerContext context, SessionManager sessionManager, CancellationToken token)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                if (settings.enableCors)
                {
                    response.Headers.Add("Access-Control-Allow-Origin", "*");
                    response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                    response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, Mcp-Session-Id");
                }

                switch (request.HttpMethod)
                {
                    case "OPTIONS":
                        response.StatusCode = 200;
                        break;

                    case "POST":
                        await HandleMcpPostRequest(request, response, sessionManager, token);
                        break;

                    case "GET":
                        await HandleGetRequest(response);
                        break;

                    default:
                        response.StatusCode = 405;
                        response.ContentType = "application/json";
                        var errorJson = "{\"error\": \"Method not allowed\"}";
                        await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(errorJson), token);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogError($"Request processing error: {ex.Message}");
                try
                {
                    response.StatusCode = 500;
                    response.ContentType = "application/json";
                    var errorMessage = ex.Message.Replace("\"", "\\\"");
                    var errorJson = $"{{\"error\": \"Internal server error: {errorMessage}\"}}";
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(errorJson), token);
                }
                catch { }
            }
            finally
            {
                try { response.Close(); } catch { }
            }
        }

        /// <summary>MCPのPOSTリクエストを処理します</summary>
        async UniTask HandleMcpPostRequest(HttpListenerRequest request, HttpListenerResponse response, SessionManager sessionManager, CancellationToken token)
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

            JsonRpcRequest jsonRpcRequest;
            try
            {
                jsonRpcRequest = JsonSerializer.Deserialize<JsonRpcRequest>(inputBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
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

            // セッションIDを取得または生成
            var sessionId = request.Headers["Mcp-Session-Id"] ?? sessionManager.CreateSession();
            var mcpSession = sessionManager.GetOrCreateSession(sessionId);

            // MCPサーバーインスタンスを取得または作成
            if (mcpSession.McpServer == null)
            {
                var inputStream = new MemoryStream();
                var outputStream = new MemoryStream();
                mcpSession.McpServer = new SimpleMcpServer(inputStream, outputStream, serviceContainer);
                mcpSession.InputStream = inputStream;
                mcpSession.OutputStream = outputStream;
            }

            // リクエストを処理
            var mcpResponse = await ProcessMcpRequest(jsonRpcRequest, mcpSession, token);

            // レスポンスをシリアライズ
            var responseJson = JsonSerializer.Serialize(mcpResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            if (settings.debugMode)
            {
                Log($"Sending response: {responseJson}");
            }

            // レスポンスヘッダーを設定
            response.StatusCode = 200;
            response.ContentType = "application/json";
            response.Headers.Add("Mcp-Session-Id", sessionId);

            await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(responseJson), token);
        }

        /// <summary>MCPリクエストを直接処理します</summary>
        async UniTask<JsonRpcResponse> ProcessMcpRequest(JsonRpcRequest request, McpSession session, CancellationToken token)
        {
            try
            {
                object result = null;

                switch (request.Method)
                {
                    case "initialize":
                        result = HandleInitialize(request);
                        break;

                    case "initialized":
                        return new JsonRpcResponse { Id = request.Id, Result = "ok" };

                    case "notifications/initialized":
                        return new JsonRpcResponse { Id = request.Id, Result = "ok" };

                    case "tools/list":
                        result = HandleListTools();
                        break;

                    case "tools/call":
                        result = await HandleCallTool(request, token);
                        break;

                    default:
                        return new JsonRpcResponse
                        {
                            Id = request.Id,
                            Error = new JsonRpcError
                            {
                                Code = JsonRpcErrorCodes.MethodNotFound,
                                Message = "Method not found"
                            }
                        };
                }

                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError
                    {
                        Code = JsonRpcErrorCodes.InternalError,
                        Message = ex.Message
                    }
                };
            }
        }

        /// <summary>initialize メソッドを処理します</summary>
        InitializeResult HandleInitialize(JsonRpcRequest request)
        {
            return new InitializeResult
            {
                ProtocolVersion = "0.1.0",
                Capabilities = new ServerCapabilities
                {
                    Tools = new { }
                },
                ServerInfo = new ServerInfo
                {
                    Name = "uMCP for Unity",
                    Version = UMcpSettings.Version
                }
            };
        }

        /// <summary>tools/list メソッドを処理します</summary>
        ListToolsResult HandleListTools()
        {
            var tools = new List<ToolInfo>();

            if (settings.enableDefaultTools)
            {
                // ビルトインツールを追加
                tools.AddRange(GetBuiltinToolInfo());
            }

            // カスタムツールを追加
            tools.AddRange(GetCustomToolInfo());

            return new ListToolsResult { Tools = tools };
        }

        /// <summary>ビルトインツールの情報を取得します</summary>
        List<ToolInfo> GetBuiltinToolInfo()
        {
            return new List<ToolInfo>
            {
                new ToolInfo { Name = "get_unity_info", Description = "Unity エディターとプロジェクトの詳細情報を取得", InputSchema = new Dictionary<string, object> { ["type"] = "object", ["properties"] = new Dictionary<string, object>() } },
                new ToolInfo { Name = "get_scene_info", Description = "現在のシーン構造とGameObject分析", InputSchema = new Dictionary<string, object> { ["type"] = "object", ["properties"] = new Dictionary<string, object>() } },
                new ToolInfo { Name = "log_message", Description = "Unity コンソールへのログ出力", InputSchema = CreateLogMessageSchema() },
                new ToolInfo { Name = "refresh_assets", Description = "アセットデータベースのリフレッシュ", InputSchema = new Dictionary<string, object> { ["type"] = "object", ["properties"] = new Dictionary<string, object>() } },
                new ToolInfo { Name = "save_project", Description = "プロジェクトとアセットの保存", InputSchema = new Dictionary<string, object> { ["type"] = "object", ["properties"] = new Dictionary<string, object>() } },
                new ToolInfo { Name = "find_assets", Description = "フィルターによるアセット検索", InputSchema = CreateFindAssetsSchema() },
                new ToolInfo { Name = "get_asset_info", Description = "アセットの詳細情報取得", InputSchema = CreateGetAssetInfoSchema() },
                new ToolInfo { Name = "reimport_asset", Description = "指定アセットの強制再インポート", InputSchema = CreateReimportAssetSchema() },
                new ToolInfo { Name = "get_console_logs", Description = "Unity コンソールログの取得とフィルタリング", InputSchema = CreateGetConsoleLogsSchema() },
                new ToolInfo { Name = "clear_console_logs", Description = "コンソールログの全クリア", InputSchema = new Dictionary<string, object> { ["type"] = "object", ["properties"] = new Dictionary<string, object>() } },
                new ToolInfo { Name = "log_to_console", Description = "カスタムメッセージのコンソール出力", InputSchema = CreateLogToConsoleSchema() },
                new ToolInfo { Name = "get_log_statistics", Description = "ログ統計情報の取得", InputSchema = new Dictionary<string, object> { ["type"] = "object", ["properties"] = new Dictionary<string, object>() } },
                new ToolInfo { Name = "run_edit_mode_tests", Description = "EditModeテストの実行と結果取得", InputSchema = CreateRunTestsSchema() },
                new ToolInfo { Name = "run_play_mode_tests", Description = "PlayModeテストの高速実行", InputSchema = CreateRunTestsSchema() },
                new ToolInfo { Name = "get_available_tests", Description = "利用可能なテスト一覧の取得", InputSchema = CreateGetAvailableTestsSchema() }
            };
        }

        /// <summary>カスタムツールの情報を取得します</summary>
        List<ToolInfo> GetCustomToolInfo()
        {
            var tools = new List<ToolInfo>();
            // TODO: カスタムツールの実装
            return tools;
        }

        /// <summary>tools/call メソッドを処理します</summary>
        async UniTask<CallToolResult> HandleCallTool(JsonRpcRequest request, CancellationToken token)
        {
            try
            {
                var paramsElement = JsonSerializer.SerializeToElement(request.Params);
                var callRequest = JsonSerializer.Deserialize<CallToolRequest>(paramsElement.GetRawText());

                // メインスレッドに切り替え
                await UniTask.SwitchToMainThread();

                // ツールを実行
                var result = await ExecuteTool(callRequest.Name, callRequest.Arguments, token);

                return new CallToolResult
                {
                    IsError = false,
                    Content = new List<ToolResultContent>
                    {
                        new ToolResultContent
                        {
                            Type = "text",
                            Text = result
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ToolResultContent>
                    {
                        new ToolResultContent
                        {
                            Type = "text",
                            Text = $"Error executing tool: {ex.Message}"
                        }
                    }
                };
            }
        }

        /// <summary>ツールを実行します</summary>
        async UniTask<string> ExecuteTool(string toolName, Dictionary<string, object> arguments, CancellationToken token)
        {
            // 引数をJSONに変換して汎用的に処理
            var argsJson = arguments != null ? JsonSerializer.Serialize(arguments) : "{}";

            switch (toolName)
            {
                case "get_unity_info":
                    var unityInfoTool = new Tools.UnityInfoToolImplementation();
                    var unityInfo = await unityInfoTool.GetUnityInfo();
                    return JsonSerializer.Serialize(unityInfo);

                case "get_scene_info":
                    var sceneInfoTool = new Tools.UnityInfoToolImplementation();
                    var sceneInfo = await sceneInfoTool.GetSceneInfo();
                    return JsonSerializer.Serialize(sceneInfo);

                case "log_message":
                    var logTool = new Tools.UnityInfoToolImplementation();
                    var message = arguments?.ContainsKey("message") == true ? arguments["message"].ToString() : "";
                    var logType = arguments?.ContainsKey("logType") == true ? arguments["logType"].ToString() : "log";
                    var logResult = await logTool.LogMessage(message, logType);
                    return JsonSerializer.Serialize(logResult);

                case "refresh_assets":
                    var assetTool = new Tools.AssetManagementToolImplementation();
                    var refreshResult = await assetTool.RefreshAssets();
                    return JsonSerializer.Serialize(refreshResult);

                case "save_project":
                    var saveTool = new Tools.AssetManagementToolImplementation();
                    var saveResult = await saveTool.SaveProject();
                    return JsonSerializer.Serialize(saveResult);

                case "get_console_logs":
                    var consoleTool = new Tools.ConsoleLogToolImplementation();
                    var maxCount = arguments?.ContainsKey("maxCount") == true ? 
                        Convert.ToInt32(arguments["maxCount"]) : 20;
                    var errorsOnly = arguments?.ContainsKey("errorsOnly") == true ? 
                        Convert.ToBoolean(arguments["errorsOnly"]) : false;
                    var includeWarnings = arguments?.ContainsKey("includeWarnings") == true ? 
                        Convert.ToBoolean(arguments["includeWarnings"]) : true;
                    var maxMessageLength = arguments?.ContainsKey("maxMessageLength") == true ? 
                        Convert.ToInt32(arguments["maxMessageLength"]) : 500;
                    var consoleResult = await consoleTool.GetConsoleLogs(maxCount, errorsOnly, includeWarnings, maxMessageLength);
                    return JsonSerializer.Serialize(consoleResult);

                case "clear_console_logs":
                    var clearTool = new Tools.ConsoleLogToolImplementation();
                    var clearResult = await clearTool.ClearConsoleLogs();
                    return JsonSerializer.Serialize(clearResult);

                // 他のツールも同様に実装
                default:
                    throw new ArgumentException($"Unknown tool: {toolName}");
            }
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

        /// <summary>各種ツールのスキーマ生成メソッド</summary>
        Dictionary<string, object> CreateLogMessageSchema() => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["message"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "ログメッセージ" },
                ["logType"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "ログタイプ (Info, Warning, Error)", ["default"] = "Info" }
            },
            ["required"] = new[] { "message" }
        };

        Dictionary<string, object> CreateFindAssetsSchema() => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["filter"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "検索フィルター" },
                ["searchInFolders"] = new Dictionary<string, object> { ["type"] = "array", ["items"] = new Dictionary<string, object> { ["type"] = "string" }, ["description"] = "検索対象フォルダ" }
            }
        };

        Dictionary<string, object> CreateGetAssetInfoSchema() => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["assetPath"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "アセットパス" }
            },
            ["required"] = new[] { "assetPath" }
        };

        Dictionary<string, object> CreateReimportAssetSchema() => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["assetPath"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "再インポートするアセットパス" }
            },
            ["required"] = new[] { "assetPath" }
        };

        Dictionary<string, object> CreateGetConsoleLogsSchema() => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["logType"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "ログタイプフィルター" },
                ["maxCount"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "最大取得数", ["default"] = 100 }
            }
        };

        Dictionary<string, object> CreateLogToConsoleSchema() => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["message"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "ログメッセージ" },
                ["logType"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "ログタイプ", ["default"] = "Log" }
            },
            ["required"] = new[] { "message" }
        };

        Dictionary<string, object> CreateRunTestsSchema() => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["testFilter"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "テストフィルター" },
                ["disableDomainReload"] = new Dictionary<string, object> { ["type"] = "boolean", ["description"] = "ドメインリロード無効化", ["default"] = true }
            }
        };

        Dictionary<string, object> CreateGetAvailableTestsSchema() => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["testMode"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "テストモード (EditMode, PlayMode, All)", ["default"] = "All" }
            }
        };

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
