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
        [McpServerTool, Description("現在のUnityコンソールログを読みやすい形式で取得")]
        public async ValueTask<object> GetConsoleLogs(
            [Description("取得する最大ログ数 (1-50)")] int maxLogs = 20,
            [Description("エラーログのみ取得するか")] bool errorsOnly = false,
            [Description("警告ログを含めるか")] bool includeWarnings = true,
            [Description("メッセージの最大文字数 (200-3000)")] int maxMessageLength = 1000)
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
            maxMessageLength = Math.Max(200, Math.Min(maxMessageLength, 3000));

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

            // 読みやすい形式のサマリーを作成
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("=== Unity コンソールログ ===");
            summary.AppendLine($"**取得条件:** {(errorsOnly ? "エラーのみ" : includeWarnings ? "全てのログ" : "エラーとログのみ")}");
            summary.AppendLine($"**統計:** エラー {errorCount}件, 警告 {warningCount}件, 情報 {infoCount}件");
            summary.AppendLine($"**表示:** {logs.Count}件（全{totalCount}件中）");
            summary.AppendLine();

            if (logs.Count > 0)
            {
                summary.AppendLine("**最新のログ:**");
                foreach (var log in logs.TakeLast(Math.Min(logs.Count, 10)))
                {
                    var icon = log.Type switch
                    {
                        "Error" => "❌",
                        "Warning" => "⚠️",
                        _ => "ℹ️"
                    };

                    // メッセージの最初の行のみを表示し、改行で分割
                    var firstLine = log.Message.Split('\n')[0];
                    if (firstLine.Length > 100)
                    {
                        firstLine = firstLine.Substring(0, 100) + "...";
                    }

                    summary.AppendLine($"{icon} **[{log.Type}]** {firstLine}");
                    
                    // 長いメッセージの場合は省略表示
                    if (log.Message.Length > firstLine.Length || log.Message.Contains('\n'))
                    {
                        summary.AppendLine($"   詳細: {log.Message.Length}文字のメッセージ");
                    }
                    summary.AppendLine();
                }
            }
            else
            {
                summary.AppendLine("**ログはありません。**");
            }

            return new
            {
                Success = true,
                FormattedOutput = summary.ToString()
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

                var info = new System.Text.StringBuilder();
                info.AppendLine("=== コンソールログクリア ===");
                info.AppendLine($"**実行時刻:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                info.AppendLine();
                info.AppendLine("## ✅ 実行結果");
                info.AppendLine("🧹 **コンソールログをすべてクリアしました**");
                info.AppendLine();
                info.AppendLine("## 💡 効果"); 
                info.AppendLine("- エラーログのクリア");
                info.AppendLine("- 警告ログのクリア");
                info.AppendLine("- 情報ログのクリア");
                info.AppendLine("- コンソールの表示がリセットされました");
                
                return new
                {
                    Success = true,
                    FormattedOutput = info.ToString()
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

            var info = new System.Text.StringBuilder();
            info.AppendLine($"=== コンソールログ出力: {logType.ToUpper()} ===");
            info.AppendLine($"**実行時刻:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine();
            
            var icon = logType.ToLower() switch
            {
                "error" => "❌",
                "warning" => "⚠️",
                _ => "ℹ️"
            };
            
            info.AppendLine("## ✅ 実行結果");
            info.AppendLine($"{icon} **メッセージをUnityコンソールに出力しました**");
            info.AppendLine();
            info.AppendLine("## 💬 出力内容");
            info.AppendLine($"**タイプ:** {logType}");
            info.AppendLine($"**メッセージ:** {message}");
            if (!string.IsNullOrEmpty(context))
            {
                info.AppendLine($"**コンテキスト:** {context}");
            }
            info.AppendLine($"**完全メッセージ:** {fullMessage}");
            
            return new
            {
                Success = true,
                FormattedOutput = info.ToString()
            };
        }

        /// <summary>コンソールログの統計情報を取得</summary>
        [McpServerTool, Description("現在のコンソールログの統計情報を読みやすい形式で取得")]
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

                var info = new System.Text.StringBuilder();
                info.AppendLine("=== コンソールログ統計 ===");
                info.AppendLine($"**分析時刻:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                info.AppendLine($"**総ログ数:** {totalCount}件");
                info.AppendLine($"**サンプルサイズ:** {sampleSize}件（最新{sampleSize}件を分析）");
                info.AppendLine();

                // 統計情報
                info.AppendLine("## 📊 ログ種別統計");
                info.AppendLine($"❌ **エラー:** {errorCount}件 ({(sampleSize > 0 ? errorCount * 100.0 / sampleSize : 0):F1}%)");
                info.AppendLine($"⚠️ **警告:** {warningCount}件 ({(sampleSize > 0 ? warningCount * 100.0 / sampleSize : 0):F1}%)"); 
                info.AppendLine($"ℹ️ **情報:** {infoCount}件 ({(sampleSize > 0 ? infoCount * 100.0 / sampleSize : 0):F1}%)");
                info.AppendLine();

                // 健全性評価
                info.AppendLine("## 🔍 ログ健全性評価");
                if (errorCount == 0 && warningCount == 0)
                {
                    info.AppendLine("✅ **優良**: エラーや警告がありません");
                }
                else if (errorCount == 0 && warningCount > 0)
                {
                    info.AppendLine("⚠️ **注意**: 警告があります（エラーなし）");
                }
                else if (errorCount > 0)
                {
                    info.AppendLine("❌ **要対応**: エラーが発生しています");
                }

                // 推奨アクション
                if (errorCount > 0 || warningCount > 0)
                {
                    info.AppendLine();
                    info.AppendLine("## 💡 推奨アクション");
                    if (errorCount > 0)
                    {
                        info.AppendLine("- `get_console_logs errorsOnly=true` でエラー詳細を確認");
                    }
                    if (warningCount > 0)
                    {
                        info.AppendLine("- `get_console_logs includeWarnings=true` で警告を確認");
                    }
                }

                return new
                {
                    Success = true,
                    FormattedOutput = info.ToString()
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
