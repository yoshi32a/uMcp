using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using uMCP.Editor.Core.Attributes;
using UnityEditor;

namespace uMCP.Editor.Tools
{
    /// <summary>エディタ拡張メソッドの実行を行うツール</summary>
    [McpServerToolType, Description("エディタ拡張メソッドの実行を行うツール")]
    internal sealed class EditorExtensionToolImplementation
    {
        /// <summary>コンパイル後のエディタメソッドを実行</summary>
        [McpServerTool, Description("コンパイル済みエディタ拡張の静的メソッドを実行")]
        public async ValueTask<object> ExecuteEditorMethod(
            [Description("完全なクラス名（名前空間含む）")] string className,
            [Description("実行するメソッド名")] string methodName,
            [Description("メソッドパラメータ（JSON形式）")] Dictionary<string, object> parameters = null)
        {
            await UniTask.SwitchToMainThread();

            try
            {
                if (string.IsNullOrEmpty(className))
                {
                    return new { success = false, error = "Class name is required" };
                }

                if (string.IsNullOrEmpty(methodName))
                {
                    return new { success = false, error = "Method name is required" };
                }

                // コンパイル状態をチェック
                if (EditorApplication.isCompiling)
                {
                    return new { 
                        success = false, 
                        error = "Unity is currently compiling scripts. Please wait for compilation to complete and try again.",
                        isCompiling = true
                    };
                }

                // コンパイルエラーをチェック
                var compilationMessages = GetCompilationErrors();
                if (compilationMessages.Length > 0)
                {
                    return new { 
                        success = false, 
                        error = "Compilation errors detected. Please fix the errors before executing methods.",
                        compilationErrors = compilationMessages,
                        errorCount = compilationMessages.Length
                    };
                }

                // 型を取得
                var type = GetTypeFromAllAssemblies(className);
                if (type == null)
                {
                    return new { 
                        success = false, 
                        error = $"Class '{className}' not found. Make sure the script is compiled.",
                        availableTypes = GetAvailableEditorTypes().Take(10).ToArray()
                    };
                }

                // メソッドを取得
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                var method = methods.FirstOrDefault(m => m.Name == methodName);
                
                if (method == null)
                {
                    return new { 
                        success = false, 
                        error = $"Method '{methodName}' not found in class '{className}'",
                        availableMethods = methods.Select(m => new {
                            name = m.Name,
                            isStatic = m.IsStatic,
                            parameters = m.GetParameters().Select(p => new { 
                                name = p.Name, 
                                type = p.ParameterType.Name,
                                hasDefault = p.HasDefaultValue,
                                defaultValue = p.HasDefaultValue ? p.DefaultValue : null
                            }).ToArray()
                        }).ToArray()
                    };
                }

                // パラメータを準備
                var methodParams = method.GetParameters();
                var args = new object[methodParams.Length];

                for (int i = 0; i < methodParams.Length; i++)
                {
                    var param = methodParams[i];
                    if (parameters != null && parameters.TryGetValue(param.Name, out var value))
                    {
                        args[i] = ConvertParameter(value, param.ParameterType);
                    }
                    else if (param.HasDefaultValue)
                    {
                        args[i] = param.DefaultValue;
                    }
                    else
                    {
                        args[i] = GetDefaultValue(param.ParameterType);
                    }
                }

                // メソッド実行
                object result;
                if (method.IsStatic)
                {
                    result = method.Invoke(null, args);
                }
                else
                {
                    // インスタンスメソッドの場合はインスタンスを作成
                    var instance = Activator.CreateInstance(type);
                    result = method.Invoke(instance, args);
                }

                // 非同期メソッドの場合
                if (result is Task task)
                {
                    await task;
                    if (task.GetType().IsGenericType)
                    {
                        var resultProperty = task.GetType().GetProperty("Result");
                        result = resultProperty?.GetValue(task);
                    }
                    else
                    {
                        result = "Method completed successfully";
                    }
                }

                return new
                {
                    success = true,
                    message = $"Method '{methodName}' executed successfully",
                    result = result,
                    executedMethod = new
                    {
                        className,
                        methodName,
                        isStatic = method.IsStatic,
                        parametersUsed = methodParams.Select((p, i) => new { 
                            name = p.Name, 
                            type = p.ParameterType.Name,
                            value = args[i] 
                        }).ToArray()
                    }
                };
            }
            catch (Exception ex)
            {
                return new { 
                    success = false, 
                    error = ex.Message, 
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message 
                };
            }
        }

        /// <summary>すべてのアセンブリから型を取得</summary>
        Type GetTypeFromAllAssemblies(string typeName)
        {
            // まず現在のアセンブリから探す
            var type = Type.GetType(typeName);
            if (type != null) return type;

            // すべてのアセンブリから探す
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null) return type;
            }

            return null;
        }

        /// <summary>利用可能なエディタ拡張型を取得</summary>
        string[] GetAvailableEditorTypes()
        {
            var editorAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name.Contains("Editor") || 
                           a.GetName().Name.StartsWith("Assembly-CSharp-Editor"));

            var types = new List<string>();
            foreach (var assembly in editorAssemblies)
            {
                try
                {
                    types.AddRange(assembly.GetTypes()
                        .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract)
                        .Select(t => t.FullName));
                }
                catch (ReflectionTypeLoadException)
                {
                    // 一部の型が読み込めない場合は無視
                }
            }

            return types.ToArray();
        }

        /// <summary>パラメータを適切な型に変換</summary>
        object ConvertParameter(object value, Type targetType)
        {
            if (value == null) return GetDefaultValue(targetType);

            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            // 基本的な型変換1
            if (targetType == typeof(string))
                return value.ToString();
            if (targetType == typeof(int))
                return Convert.ToInt32(value);
            if (targetType == typeof(bool))
                return Convert.ToBoolean(value);
            if (targetType == typeof(float))
                return Convert.ToSingle(value);
            if (targetType == typeof(double))
                return Convert.ToDouble(value);

            return GetDefaultValue(targetType);
        }

        /// <summary>型のデフォルト値を取得</summary>
        object GetDefaultValue(Type type)
        {
            if (type == typeof(string)) return "";
            if (type.IsValueType) return Activator.CreateInstance(type);
            return null;
        }

        /// <summary>コンパイルエラーを取得</summary>
        string[] GetCompilationErrors()
        {
            var errors = new List<string>();

            // シンプルに現在のコンパイル状態をチェック
            // 実際のエラー詳細はコンソールログツールで確認可能
            if (EditorApplication.isCompiling)
            {
                errors.Add("Scripts are currently being compiled. Please wait for compilation to complete.");
            }

            return errors.ToArray();
        }
    }
}
