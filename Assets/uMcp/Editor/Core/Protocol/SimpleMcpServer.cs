using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using uMCP.Editor.Core.Attributes;
using uMCP.Editor.Core.DependencyInjection;
using UnityEngine;

namespace uMCP.Editor.Core.Protocol
{
    /// <summary>シンプルなMCPサーバー実装</summary>
    public class SimpleMcpServer : IMcpServer
    {
        private readonly Stream inputStream;
        private readonly Stream outputStream;
        private readonly SimpleServiceContainer container;
        private readonly Dictionary<string, ToolMethod> tools = new Dictionary<string, ToolMethod>();
        private readonly JsonSerializerOptions jsonOptions;

        private class ToolMethod
        {
            public object Instance { get; set; }
            public MethodInfo Method { get; set; }
            public string Description { get; set; }
            public Dictionary<string, object> InputSchema { get; set; }
        }

        public SimpleMcpServer(Stream inputStream, Stream outputStream, SimpleServiceContainer container)
        {
            this.inputStream = inputStream;
            this.outputStream = outputStream;
            this.container = container;

            jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            DiscoverTools();
        }

        private void DiscoverTools()
        {
            // すべてのサービスからツールを探す
            var serviceTypes = container.Services;

            if (serviceTypes == null) return;

            foreach (var kvp in serviceTypes)
            {
                var type = kvp.Key;
                var instance = kvp.Value;

                // McpServerToolType属性を持つクラスを探す
                if (type.GetCustomAttribute<McpServerToolTypeAttribute>() == null)
                    continue;

                // McpServerTool属性を持つメソッドを探す
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    var toolAttr = method.GetCustomAttribute<McpServerToolAttribute>();
                    if (toolAttr == null) continue;

                    var descAttr = method.GetCustomAttribute<DescriptionAttribute>();
                    var toolName = ConvertToSnakeCase(method.Name);

                    // パラメータからスキーマを生成
                    var inputSchema = GenerateInputSchema(method);

                    tools[toolName] = new ToolMethod
                    {
                        Instance = instance,
                        Method = method,
                        Description = descAttr?.Description ?? "",
                        InputSchema = inputSchema
                    };
                }
            }
        }

        private string ConvertToSnakeCase(string name)
        {
            // PascalCase を snake_case に変換
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

        private Dictionary<string, object> GenerateInputSchema(MethodInfo method)
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
                    ["type"] = GetJsonType(param.ParameterType)
                };

                var descAttr = param.GetCustomAttribute<DescriptionAttribute>();
                if (descAttr != null)
                {
                    paramSchema["description"] = descAttr.Description;
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

        private string GetJsonType(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int) || type == typeof(long)) return "integer";
            if (type == typeof(float) || type == typeof(double)) return "number";
            if (type == typeof(bool)) return "boolean";
            if (type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return "array";
            return "object";
        }

        public async UniTask RunAsync(CancellationToken cancellationToken)
        {
            var reader = new StreamReader(inputStream, Encoding.UTF8);
            var writer = new StreamWriter(outputStream, Encoding.UTF8) { AutoFlush = true };

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    JsonRpcResponse response;
                    try
                    {
                        var request = JsonSerializer.Deserialize<JsonRpcRequest>(line, jsonOptions);
                        response = await HandleRequest(request, cancellationToken);
                    }
                    catch (JsonException)
                    {
                        response = new JsonRpcResponse
                        {
                            Error = new JsonRpcError
                            {
                                Code = JsonRpcErrorCodes.ParseError,
                                Message = "Parse error"
                            }
                        };
                    }

                    var responseJson = JsonSerializer.Serialize(response, jsonOptions);
                    await writer.WriteLineAsync(responseJson);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[uMCP] Server error: {ex.Message}");
            }
        }

        private async Task<JsonRpcResponse> HandleRequest(JsonRpcRequest request, CancellationToken cancellationToken)
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
                        // 通知なので結果は返さない
                        return new JsonRpcResponse { Id = request.Id, Result = "ok" };

                    case "tools/list":
                        result = HandleListTools();
                        break;

                    case "tools/call":
                        result = await HandleCallTool(request, cancellationToken);
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

        private InitializeResult HandleInitialize(JsonRpcRequest request)
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

        private ListToolsResult HandleListTools()
        {
            var toolInfos = tools.Select(kvp => new ToolInfo
            {
                Name = kvp.Key,
                Description = kvp.Value.Description,
                InputSchema = kvp.Value.InputSchema
            }).ToList();

            return new ListToolsResult { Tools = toolInfos };
        }

        private async Task<CallToolResult> HandleCallTool(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            var paramsElement = JsonSerializer.SerializeToElement(request.Params, jsonOptions);
            var callRequest = JsonSerializer.Deserialize<CallToolRequest>(paramsElement.GetRawText(), jsonOptions);

            if (!tools.TryGetValue(callRequest.Name, out var toolMethod))
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ToolResultContent>
                    {
                        new ToolResultContent
                        {
                            Type = "text",
                            Text = $"Tool '{callRequest.Name}' not found"
                        }
                    }
                };
            }

            try
            {
                // メソッドのパラメータを準備
                var parameters = toolMethod.Method.GetParameters();
                var args = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    if (param.ParameterType == typeof(CancellationToken))
                    {
                        args[i] = cancellationToken;
                    }
                    else if (callRequest.Arguments != null && callRequest.Arguments.TryGetValue(param.Name, out var value))
                    {
                        if (value is JsonElement jsonElement)
                        {
                            args[i] = JsonSerializer.Deserialize(jsonElement.GetRawText(), param.ParameterType, jsonOptions);
                        }
                        else
                        {
                            args[i] = Convert.ChangeType(value, param.ParameterType);
                        }
                    }
                    else if (param.HasDefaultValue)
                    {
                        args[i] = param.DefaultValue;
                    }
                    else
                    {
                        throw new ArgumentException($"Required parameter '{param.Name}' not provided");
                    }
                }

                // メソッドを実行
                var result = toolMethod.Method.Invoke(toolMethod.Instance, args);

                // 非同期メソッドの場合
                if (result is Task task)
                {
                    await task;
                    var taskType = task.GetType();
                    if (taskType.IsGenericType)
                    {
                        result = taskType.GetProperty("Result").GetValue(task);
                    }
                    else
                    {
                        result = null;
                    }
                }
                else if (result is ValueTask valueTask)
                {
                    await valueTask.AsTask();
                    result = null;
                }
                else if (result != null && result.GetType().IsGenericType &&
                         result.GetType().GetGenericTypeDefinition() == typeof(ValueTask<>))
                {
                    var asTaskMethod = result.GetType().GetMethod("AsTask");
                    var task2 = (Task)asTaskMethod.Invoke(result, null);
                    await task2;
                    result = task2.GetType().GetProperty("Result").GetValue(task2);
                }

                // 結果をJSON文字列に変換
                var resultJson = result != null ? JsonSerializer.Serialize(result, jsonOptions) : "null";

                return new CallToolResult
                {
                    IsError = false,
                    Content = new List<ToolResultContent>
                    {
                        new ToolResultContent
                        {
                            Type = "text",
                            Text = resultJson
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
    }
}
