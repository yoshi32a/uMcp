using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using uMCP.Editor.Core.Attributes;
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

        /// <summary>共通のJSON設定</summary>
        static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

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
                    HttpListenerContext context;
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
                        var errorResponse = new JsonRpcError { Code = -32601, Message = "Method not allowed" };
                        var errorJson = JsonSerializer.Serialize(errorResponse, jsonOptions);
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
                    var errorResponse = new JsonRpcError { Code = JsonRpcErrorCodes.InternalError, Message = $"Internal server error: {ex.Message}" };
                    var errorJson = JsonSerializer.Serialize(errorResponse, jsonOptions);
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(errorJson), token);
                }
                catch
                {
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
                }
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
                var errorResponse = new JsonRpcError { Code = JsonRpcErrorCodes.InvalidRequest, Message = "Empty request body" };
                var errorJson = JsonSerializer.Serialize(errorResponse, jsonOptions);
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(errorJson), token);
                return;
            }

            JsonRpcRequest jsonRpcRequest;
            try
            {
                jsonRpcRequest = JsonSerializer.Deserialize<JsonRpcRequest>(inputBody, jsonOptions);
            }
            catch (Exception ex)
            {
                response.StatusCode = 400;
                response.ContentType = "application/json";
                var errorResponse = new JsonRpcError { Code = JsonRpcErrorCodes.ParseError, Message = $"Invalid JSON: {ex.Message}" };
                var errorJson = JsonSerializer.Serialize(errorResponse, jsonOptions);
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
            var responseJson = JsonSerializer.Serialize(mcpResponse, jsonOptions);

            if (settings.debugMode)
            {
                Log($"Sending response: {responseJson}");
            }

            // レスポンスヘッダーを設定
            response.StatusCode = 200;
            response.ContentType = "application/json; charset=utf-8";
            response.Headers.Add("Mcp-Session-Id", sessionId);

            // Streamable HTTP用の追加ヘッダー
            response.Headers.Add("Cache-Control", "no-cache");
            response.Headers.Add("Connection", "keep-alive");

            var responseBytes = Encoding.UTF8.GetBytes(responseJson);
            response.ContentLength64 = responseBytes.Length;
            await response.OutputStream.WriteAsync(responseBytes, token);
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
                ProtocolVersion = "2024-11-05",
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
            var tools = new List<ToolInfo>();

            // サービスコンテナからツール情報を自動生成
            foreach (var serviceKvp in serviceContainer.Services)
            {
                var serviceType = serviceKvp.Key;

                // McpServerToolType属性があるかチェック
                if (serviceType.GetCustomAttribute<McpServerToolTypeAttribute>() == null)
                    continue;

                // 各メソッドを確認
                foreach (var method in serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    var toolAttr = method.GetCustomAttribute<McpServerToolAttribute>();
                    if (toolAttr == null) continue;

                    var descAttr = method.GetCustomAttribute<DescriptionAttribute>();
                    var toolName = ConvertToSnakeCase(method.Name);
                    var inputSchema = GenerateInputSchema(method);

                    tools.Add(new ToolInfo
                    {
                        Name = toolName,
                        Description = descAttr?.Description ?? "",
                        InputSchema = inputSchema
                    });
                }
            }

            return tools;
        }

        /// <summary>メソッドからInputSchemaを生成します</summary>
        Dictionary<string, object> GenerateInputSchema(MethodInfo method)
        {
            var parameters = method.GetParameters();
            var properties = new Dictionary<string, object>();
            var required = new List<string>();

            foreach (var param in parameters)
            {
                if (param.ParameterType == typeof(CancellationToken))
                    continue;

                var paramSchema = new Dictionary<string, object>
                {
                    ["type"] = GetJsonSchemaType(param.ParameterType)
                };

                var descAttr = param.GetCustomAttribute<DescriptionAttribute>();
                if (descAttr != null)
                {
                    paramSchema["description"] = descAttr.Description;
                }

                if (param.HasDefaultValue && param.DefaultValue != null)
                {
                    paramSchema["default"] = param.DefaultValue;
                }

                properties[param.Name] = paramSchema;

                if (!param.HasDefaultValue)
                {
                    required.Add(param.Name);
                }
            }

            var schema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties
            };

            if (required.Count > 0)
            {
                schema["required"] = required;
            }

            return schema;
        }

        /// <summary>型からJSONスキーマタイプを取得します</summary>
        string GetJsonSchemaType(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int) || type == typeof(long)) return "integer";
            if (type == typeof(float) || type == typeof(double)) return "number";
            if (type == typeof(bool)) return "boolean";
            if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))) return "array";
            return "object";
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
                        new()
                        {
                            Type = "text",
                            Text = result
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                // リフレクション例外の場合は内部例外を取得
                var actualException = ex is System.Reflection.TargetInvocationException tie && tie.InnerException != null
                    ? tie.InnerException
                    : ex;

                LogError($"Tool execution error - Error: {actualException}");

                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ToolResultContent>
                    {
                        new()
                        {
                            Type = "text",
                            Text = $"Error executing tool: {actualException.Message}\nStackTrace: {actualException.StackTrace}"
                        }
                    }
                };
            }
        }

        /// <summary>ツールを実行します</summary>
        async UniTask<string> ExecuteTool(string toolName, Dictionary<string, object> arguments, CancellationToken token)
        {
            // サービスコンテナから適切なツールインスタンスを取得して実行
            var toolResult = await ExecuteToolUsingReflection(toolName, arguments, token);
            return JsonSerializer.Serialize(toolResult);
        }

        /// <summary>リフレクションを使用してツールを自動実行します</summary>
        async UniTask<object> ExecuteToolUsingReflection(string toolName, Dictionary<string, object> arguments, CancellationToken token)
        {
            // サービスコンテナからすべてのツール実装を取得
            foreach (var serviceKvp in serviceContainer.Services)
            {
                var serviceType = serviceKvp.Key;
                var serviceInstance = serviceKvp.Value;

                // McpServerToolType属性があるかチェック
                if (serviceType.GetCustomAttribute<McpServerToolTypeAttribute>() == null)
                    continue;

                // 該当するメソッドを探す
                foreach (var method in serviceType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    var toolAttr = method.GetCustomAttribute<McpServerToolAttribute>();
                    if (toolAttr == null) continue;

                    var methodToolName = ConvertToSnakeCase(method.Name);
                    if (methodToolName != toolName)
                        continue;

                    // メソッドのパラメータを準備
                    var parameters = method.GetParameters();
                    var args = new object[parameters.Length];

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        if (param.ParameterType == typeof(CancellationToken))
                        {
                            args[i] = token;
                        }
                        else if (arguments != null && arguments.ContainsKey(param.Name))
                        {
                            args[i] = ConvertArgument(arguments[param.Name], param.ParameterType);
                        }
                        else if (param.HasDefaultValue)
                        {
                            args[i] = param.DefaultValue;
                        }
                        else
                        {
                            // 必須パラメータの場合はデフォルト値を設定
                            args[i] = GetDefaultValue(param.ParameterType);
                        }
                    }

                    // メソッドを実行
                    var result = method.Invoke(serviceInstance, args);

                    // 非同期メソッドの場合の処理
                    if (result is ValueTask valueTask)
                    {
                        await valueTask;
                        var valueTaskType = valueTask.GetType();
                        if (valueTaskType.IsGenericType)
                        {
                            var resultProperty = valueTaskType.GetProperty("Result");
                            return resultProperty?.GetValue(valueTask);
                        }

                        return null;
                    }
                    else if (result is Task task)
                    {
                        await task;
                        var taskType = task.GetType();
                        if (taskType.IsGenericType)
                        {
                            var resultProperty = taskType.GetProperty("Result");
                            return resultProperty?.GetValue(task);
                        }

                        return null;
                    }

                    return result;
                }
            }

            throw new ArgumentException($"Unknown tool: {toolName}");
        }

        /// <summary>引数を適切な型に変換します</summary>
        object ConvertArgument(object value, Type targetType)
        {
            if (value == null) return GetDefaultValue(targetType);

            // 既に正しい型の場合
            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            // プリミティブ型の変換
            if (targetType == typeof(string))
                return value.ToString();
            else if (targetType == typeof(int))
                return int.TryParse(value.ToString(), out var intVal) ? intVal : 0;
            else if (targetType == typeof(bool))
                return bool.TryParse(value.ToString(), out var boolVal) ? boolVal : false;
            else if (targetType == typeof(double))
                return double.TryParse(value.ToString(), out var doubleVal) ? doubleVal : 0.0;
            else if (targetType.IsArray && targetType.GetElementType() == typeof(string))
            {
                // 文字列配列の場合 - JSONElementから変換
                if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<string>();
                    foreach (var item in jsonElement.EnumerateArray())
                    {
                        list.Add(item.GetString());
                    }

                    return list.ToArray();
                }

                return new string[0];
            }

            // デフォルト値を返す
            return GetDefaultValue(targetType);
        }

        /// <summary>型のデフォルト値を取得します</summary>
        object GetDefaultValue(Type type)
        {
            if (type == typeof(string)) return "";
            if (type == typeof(int)) return 0;
            if (type == typeof(bool)) return false;
            if (type == typeof(double)) return 0.0;
            if (type.IsArray) return Array.CreateInstance(type.GetElementType(), 0);
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        /// <summary>PascalCaseをsnake_caseに変換します</summary>
        string ConvertToSnakeCase(string name)
        {
            var result = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]))
                {
                    result.Append('_');
                }

                result.Append(char.ToLower(name[i]));
            }

            return result.ToString();
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
            var statusResponse = new JsonRpcResponse
            {
                Id = null,
                Result = new
                {
                    status = "running",
                    server = "uMCP for Unity",
                    version = UMcpSettings.Version,
                    unity_version = Application.unityVersion,
                    platform = Application.platform.ToString()
                }
            };

            var statusJson = JsonSerializer.Serialize(statusResponse, jsonOptions);

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
