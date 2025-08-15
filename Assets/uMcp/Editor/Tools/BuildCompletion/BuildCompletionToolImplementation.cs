using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using uMCP.Editor.Core.Attributes;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace uMCP.Editor.Tools
{
    /// <summary>ビルド完了確認ツールの実装</summary>
    [McpServerToolType, Description("Unityビルド状態確認ツール")]
    internal sealed class BuildCompletionToolImplementation
    {
        static BuildReport lastBuildReport;
        static DateTime lastBuildTime = DateTime.MinValue;
        static bool isBuildInProgress = false;
        
        // 静的コンストラクタを削除（ビルドイベントフックは別の方法で実装）
        
        /// <summary>手動でビルド結果を記録</summary>
        public static void RecordBuildResult(BuildReport report)
        {
            lastBuildReport = report;
            lastBuildTime = DateTime.Now;
            isBuildInProgress = false;
        }
        
        /// <summary>現在のビルド状態を取得</summary>
        [McpServerTool, Description("現在のビルド状態と最後のビルド結果を取得")]
        public async ValueTask<object> GetBuildStatus()
        {
            await UniTask.SwitchToMainThread();
            
            var info = new System.Text.StringBuilder();
            info.AppendLine("=== Unity ビルド状態 ===");
            info.AppendLine($"**確認時刻:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine();
            
            // コンパイル状態チェック
            if (EditorApplication.isCompiling)
            {
                info.AppendLine("## ⏳ コンパイル中");
                info.AppendLine("スクリプトのコンパイルが進行中です。");
                
                return new StandardResponse
                {
                    Success = true,
                    FormattedOutput = info.ToString()
                };
            }
            
            // ビルド進行中チェック
            if (isBuildInProgress)
            {
                info.AppendLine("## 🔨 ビルド実行中");
                info.AppendLine("プレイヤービルドが進行中です。");
                
                return new StandardResponse
                {
                    Success = true,
                    FormattedOutput = info.ToString()
                };
            }
            
            // 最後のビルド結果
            if (lastBuildReport != null)
            {
                info.AppendLine("## 📊 最後のビルド結果");
                info.AppendLine($"**ビルド時刻:** {lastBuildTime:yyyy-MM-dd HH:mm:ss}");
                info.AppendLine($"**結果:** {GetBuildResultEmoji(lastBuildReport.summary.result)} {lastBuildReport.summary.result}");
                info.AppendLine($"**プラットフォーム:** {lastBuildReport.summary.platform}");
                info.AppendLine($"**出力パス:** {lastBuildReport.summary.outputPath}");
                info.AppendLine($"**ビルド時間:** {lastBuildReport.summary.totalTime.TotalSeconds:F2}秒");
                info.AppendLine($"**合計サイズ:** {FormatFileSize(lastBuildReport.summary.totalSize)}");
                
                if (lastBuildReport.summary.totalErrors > 0)
                {
                    info.AppendLine($"**エラー数:** ❌ {lastBuildReport.summary.totalErrors}");
                }
                if (lastBuildReport.summary.totalWarnings > 0)
                {
                    info.AppendLine($"**警告数:** ⚠️ {lastBuildReport.summary.totalWarnings}");
                }
                
                return new StandardResponse
                {
                    Success = true,
                    FormattedOutput = info.ToString()
                };
            }
            
            info.AppendLine("## ℹ️ ビルド情報なし");
            info.AppendLine("まだビルドが実行されていません。");
            
            return new StandardResponse
            {
                Success = true,
                FormattedOutput = info.ToString()
            };
        }
        
        /// <summary>ビルドの完了を待機</summary>
        [McpServerTool, Description("ビルドの完了を待機して結果を返す")]
        public async ValueTask<object> WaitForBuildCompletion(
            [Description("タイムアウト秒数")] int timeoutSeconds = 300)
        {
            await UniTask.SwitchToMainThread();
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            var startTime = DateTime.Now;
            
            // コンパイル完了を待機
            while (EditorApplication.isCompiling && !cts.Token.IsCancellationRequested)
            {
                await UniTask.Delay(500, cancellationToken: cts.Token);
            }
            
            // ビルド完了を待機
            while (isBuildInProgress && !cts.Token.IsCancellationRequested)
            {
                await UniTask.Delay(500, cancellationToken: cts.Token);
            }
            
            if (cts.Token.IsCancellationRequested)
            {
                return new StandardResponse
                {
                    Success = false,
                    Error = "タイムアウト",
                    Message = $"{timeoutSeconds}秒以内にビルドが完了しませんでした。"
                };
            }
            
            var duration = (DateTime.Now - startTime).TotalSeconds;
            
            var info = new System.Text.StringBuilder();
            info.AppendLine("=== ビルド完了 ===");
            info.AppendLine($"**待機時間:** {duration:F2}秒");
            
            if (lastBuildReport != null && lastBuildTime > startTime)
            {
                info.AppendLine($"**結果:** {GetBuildResultEmoji(lastBuildReport.summary.result)} {lastBuildReport.summary.result}");
                info.AppendLine($"**プラットフォーム:** {lastBuildReport.summary.platform}");
                info.AppendLine($"**ビルド時間:** {lastBuildReport.summary.totalTime.TotalSeconds:F2}秒");
                
                return new StandardResponse
                {
                    Success = true,
                    FormattedOutput = info.ToString()
                };
            }
            
            info.AppendLine("ビルドは完了しましたが、レポート情報がありません。");
            
            return new StandardResponse
            {
                Success = true,
                FormattedOutput = info.ToString()
            };
        }
        
        /// <summary>最後のビルドログを取得</summary>
        [McpServerTool, Description("最後のビルドの詳細ログを取得")]
        public async ValueTask<object> GetLastBuildLog(
            [Description("取得するログステップの最大数")] int maxSteps = 50)
        {
            await UniTask.SwitchToMainThread();
            
            if (lastBuildReport == null)
            {
                return new StandardResponse
                {
                    Success = false,
                    Error = "ビルド情報なし",
                    Message = "まだビルドが実行されていません。"
                };
            }
            
            var info = new System.Text.StringBuilder();
            info.AppendLine("=== 最後のビルドログ ===");
            info.AppendLine($"**ビルド時刻:** {lastBuildTime:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine($"**結果:** {GetBuildResultEmoji(lastBuildReport.summary.result)} {lastBuildReport.summary.result}");
            info.AppendLine();
            
            // ビルドステップ
            var steps = lastBuildReport.steps.Take(maxSteps).ToList();
            if (steps.Any())
            {
                info.AppendLine("## 📋 ビルドステップ");
                foreach (var step in steps)
                {
                    info.AppendLine($"- **{step.name}** ({step.duration.TotalSeconds:F2}秒)");
                    if (step.messages.Length > 0)
                    {
                        foreach (var msg in step.messages)
                        {
                            var icon = msg.type == LogType.Error ? "❌" : 
                                      msg.type == LogType.Warning ? "⚠️" : "ℹ️";
                            info.AppendLine($"  {icon} {msg.content}");
                        }
                    }
                }
            }
            
            // エラーと警告の数を表示
            if (lastBuildReport.summary.totalErrors > 0)
            {
                info.AppendLine();
                info.AppendLine($"## ❌ エラー: {lastBuildReport.summary.totalErrors}件");
            }
            
            if (lastBuildReport.summary.totalWarnings > 0)
            {
                info.AppendLine();
                info.AppendLine($"## ⚠️ 警告: {lastBuildReport.summary.totalWarnings}件");
            }
            
            return new StandardResponse
            {
                Success = true,
                FormattedOutput = info.ToString()
            };
        }
        
        /// <summary>ビルドキャッシュをクリア</summary>
        [McpServerTool, Description("ビルドキャッシュをクリアして次回フルビルドを強制")]
        public async ValueTask<object> ClearBuildCache()
        {
            await UniTask.SwitchToMainThread();
            
            var info = new System.Text.StringBuilder();
            info.AppendLine("=== ビルドキャッシュクリア ===");
            info.AppendLine($"**実行時刻:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine();
            
            try
            {
                // Library/BuildCache をクリア
                var buildCachePath = Path.Combine(Application.dataPath, "..", "Library", "BuildCache");
                if (Directory.Exists(buildCachePath))
                {
                    Directory.Delete(buildCachePath, true);
                    info.AppendLine("✅ BuildCacheディレクトリを削除しました");
                }
                else
                {
                    info.AppendLine("ℹ️ BuildCacheディレクトリは存在しませんでした");
                }
                
                // Library/il2cpp_cache をクリア (IL2CPPビルドの場合)
                var il2cppCachePath = Path.Combine(Application.dataPath, "..", "Library", "il2cpp_cache");
                if (Directory.Exists(il2cppCachePath))
                {
                    Directory.Delete(il2cppCachePath, true);
                    info.AppendLine("✅ IL2CPPキャッシュを削除しました");
                }
                
                info.AppendLine();
                info.AppendLine("## 💡 効果");
                info.AppendLine("- 次回ビルド時にフルビルドが実行されます");
                info.AppendLine("- ビルドエラーの原因となるキャッシュ問題が解決される可能性があります");
                
                return new StandardResponse
                {
                    Success = true,
                    FormattedOutput = info.ToString()
                };
            }
            catch (Exception ex)
            {
                return new StandardResponse
                {
                    Success = false,
                    Error = "キャッシュクリア失敗",
                    Message = ex.Message,
                    FormattedOutput = info.ToString()
                };
            }
        }

        static string GetBuildResultEmoji(BuildResult result)
        {
            return result switch
            {
                BuildResult.Succeeded => "✅",
                BuildResult.Failed => "❌",
                BuildResult.Cancelled => "🚫",
                BuildResult.Unknown => "❓",
                _ => "❓"
            };
        }

        static string FormatFileSize(ulong bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
