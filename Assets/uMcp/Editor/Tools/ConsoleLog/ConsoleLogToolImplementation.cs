using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using uMCP.Editor.Core.Attributes;
using UnityEngine;

namespace uMCP.Editor.Tools
{
    /// <summary>コンソールログツールの実装</summary>
    [McpServerToolType, Description("Unityコンソールログ管理ツール")]
    internal sealed class ConsoleLogToolImplementation
    {
        static readonly Type logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
        static readonly Type logEntryType = Type.GetType("UnityEditor.LogEntry,UnityEditor.dll");
        static readonly MethodInfo getCountMethod = logEntriesType?.GetMethod("GetCount", BindingFlags.Public | BindingFlags.Static);
        static readonly MethodInfo getEntryInternalMethod = logEntriesType?.GetMethod("GetEntryInternal", BindingFlags.Public | BindingFlags.Static);
        static readonly MethodInfo clearMethod = logEntriesType?.GetMethod("Clear", BindingFlags.Public | BindingFlags.Static);

        /// <summary>現在のコンソールログを取得</summary>
        [McpServerTool, Description("現在のUnityコンソールログを取得")]
        public async ValueTask<object> GetConsoleLogs(
            [Description("取得する最大ログ数 (1-50)")] int maxLogs = 20,
            [Description("エラーログのみ取得するか")] bool errorsOnly = false,
            [Description("警告ログを含めるか")] bool includeWarnings = true,
            [Description("メッセージの最大文字数 (50-2000)")] int maxMessageLength = 500)
        {
            await UniTask.SwitchToMainThread();

            if (logEntriesType == null || logEntryType == null || getCountMethod == null || getEntryInternalMethod == null)
            {
                return new ErrorResponse
                {
                    Success = false,
                    Error = "Cannot access Unity console logs (reflection failed)"
                };
            }

            // パラメータ制限
            maxLogs = Math.Max(1, Math.Min(maxLogs, 50));
            maxMessageLength = Math.Max(50, Math.Min(maxMessageLength, 2000));

            var logs = new List<LogEntry>();
            int totalCount = (int)getCountMethod.Invoke(null, null);
            int startIndex = Math.Max(0, totalCount - maxLogs);

            for (int i = startIndex; i < totalCount; i++)
            {
                try
                {
                    var logEntry = Activator.CreateInstance(logEntryType);
                    getEntryInternalMethod.Invoke(null, new[] { i, logEntry });

                    // LogEntryのフィールドからメッセージとモードを取得
                    var condition = logEntry.GetType().GetField("condition")?.GetValue(logEntry)?.ToString()
                                    ?? logEntry.GetType().GetField("message")?.GetValue(logEntry)?.ToString() ?? "";
                    var mode = (int)(logEntry.GetType().GetField("mode")?.GetValue(logEntry) ?? 0);

                    // modeによる正確な判定（Unity内部実装）
                    string logType = "Log";
                    if ((mode & (1 << 0)) != 0) logType = "Error";       // Error flag
                    else if ((mode & (1 << 1)) != 0) logType = "Warning"; // Warning flag
                    
                    // メッセージ内容からのfallback判定
                    if (logType == "Log")
                    {
                        if (condition.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                            condition.Contains("exception", StringComparison.OrdinalIgnoreCase) ||
                            condition.Contains("UnityEngine.Debug:LogError") ||
                            condition.Contains("UnityEngine.Debug:LogException"))
                            logType = "Error";
                        else if (condition.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
                                condition.Contains("UnityEngine.Debug:LogWarning"))
                            logType = "Warning";
                    }

                    // フィルタリング
                    if (errorsOnly && logType != "Error") continue;
                    if (!includeWarnings && logType == "Warning") continue;

                    // メッセージ長制限
                    var truncatedMessage = condition.Length > maxMessageLength
                        ? condition.Substring(0, maxMessageLength) + "... [truncated]"
                        : condition;

                    logs.Add(new LogEntry
                    {
                        Index = i,
                        Type = logType,
                        Message = truncatedMessage
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[uMCP] Failed to read log entry {i}: {ex.Message}");
                }
            }

            var errorCount = logs.Count(l => l.Type == "Error");
            var warningCount = logs.Count(l => l.Type == "Warning");
            var infoCount = logs.Count - errorCount - warningCount;

            return new ConsoleLogResponse
            {
                Success = true,
                TotalLogsInConsole = totalCount,
                RetrievedLogs = logs.Count,
                Summary = new LogSummary
                {
                    Errors = errorCount,
                    Warnings = warningCount,
                    Info = infoCount
                },
                Logs = logs
            };
        }

        /// <summary>コンソールログをクリア</summary>
        [McpServerTool, Description("Unityコンソールログをすべてクリア")]
        public async ValueTask<object> ClearConsoleLogs()
        {
            await UniTask.SwitchToMainThread();

            if (clearMethod == null)
            {
                return new ErrorResponse
                {
                    Success = false,
                    Error = "Cannot access Unity console clear method (reflection failed)"
                };
            }

            try
            {
                clearMethod.Invoke(null, null);

                return new ClearConsoleResponse
                {
                    Success = true,
                    Message = "Console logs cleared successfully",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }
            catch (Exception ex)
            {
                return new ErrorResponse
                {
                    Success = false,
                    Error = $"Failed to clear console logs: {ex.Message}"
                };
            }
        }

        /// <summary>Unityコンソールにログを出力</summary>
        [McpServerTool, Description("Unityコンソールに指定したメッセージをログ出力")]
        public async ValueTask<object> LogToConsole(
            [Description("ログメッセージ")] string message = "Test message",
            [Description("ログタイプ: log, warning, error")]
            string logType = "log",
            [Description("追加のコンテキスト情報")] string context = "")
        {
            await UniTask.SwitchToMainThread();

            string fullMessage = string.IsNullOrEmpty(context)
                ? $"[MCP] {message}"
                : $"[MCP] {message} (Context: {context})";

            switch (logType.ToLower())
            {
                case "error":
                    Debug.LogError(fullMessage);
                    break;
                case "warning":
                    Debug.LogWarning(fullMessage);
                    break;
                default:
                    Debug.Log(fullMessage);
                    break;
            }

            return new LogToConsoleResponse
            {
                Success = true,
                Message = $"Logged {logType}: {message}",
                FullMessage = fullMessage,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        /// <summary>コンソールログの統計情報を取得</summary>
        [McpServerTool, Description("現在のコンソールログの統計情報を取得")]
        public async ValueTask<object> GetLogStatistics()
        {
            await UniTask.SwitchToMainThread();

            if (logEntriesType == null || logEntryType == null || getCountMethod == null || getEntryInternalMethod == null)
            {
                return new ErrorResponse
                {
                    Success = false,
                    Error = "Cannot access Unity console logs (reflection failed)"
                };
            }

            try
            {
                int totalCount = (int)getCountMethod.Invoke(null, null);
                int errorCount = 0, warningCount = 0, infoCount = 0;

                // 最新100件をサンプリングして統計を取得
                int sampleSize = Math.Min(100, totalCount);
                int startIndex = Math.Max(0, totalCount - sampleSize);

                for (int i = startIndex; i < totalCount; i++)
                {
                    try
                    {
                        var logEntry = Activator.CreateInstance(logEntryType);
                        getEntryInternalMethod.Invoke(null, new[] { i, logEntry });

                        var mode = (int)(logEntry.GetType().GetField("mode")?.GetValue(logEntry) ?? 0);

                        if ((mode & (1 << 0)) != 0) errorCount++;
                        else if ((mode & (1 << 1)) != 0) warningCount++;
                        else infoCount++;
                    }
                    catch
                    {
                        // ログエントリの読み取りに失敗した場合はスキップ
                    }
                }

                return new LogStatisticsResponse
                {
                    Success = true,
                    TotalLogs = totalCount,
                    SampleSize = sampleSize,
                    Statistics = new StatisticsSummary
                    {
                        Errors = errorCount,
                        Warnings = warningCount,
                        Info = infoCount,
                        ErrorPercentage = totalCount > 0 ? errorCount * 100.0 / sampleSize : 0,
                        WarningPercentage = totalCount > 0 ? warningCount * 100.0 / sampleSize : 0
                    },
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }
            catch (Exception ex)
            {
                return new ErrorResponse
                {
                    Success = false,
                    Error = $"Failed to get log statistics: {ex.Message}"
                };
            }
        }
    }
}
