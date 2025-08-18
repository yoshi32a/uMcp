using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using uMCP.Editor.Core.Attributes;
using UnityEditor;

namespace uMCP.Editor.Tools
{
    /// <summary>アセット管理ツールの実装</summary>
    [McpServerToolType, Description("Unityアセット管理ツール")]
    internal sealed class AssetManagementToolImplementation
    {
        /// <summary>アセットデータベースをリフレッシュ（シンプル同期版）</summary>
        [McpServerTool, Description("アセットデータベースをリフレッシュして変更を反映")]
        public async ValueTask<object> RefreshAssets()
        {
            await UniTask.SwitchToMainThread();

            var startTime = DateTime.Now;

            // シンプルにリフレッシュのみ
            AssetDatabase.Refresh();

            var duration = (DateTime.Now - startTime).TotalMilliseconds;

            var info = new System.Text.StringBuilder();
            info.AppendLine("=== アセットデータベース更新 ===");
            info.AppendLine($"**実行時刻:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine($"**処理時間:** {duration:F2}ms");
            info.AppendLine();
            info.AppendLine("## ✅ 実行結果");
            info.AppendLine("📁 **アセットデータベースの更新が完了しました**");
            info.AppendLine();
            info.AppendLine("## 💡 効果");
            info.AppendLine("- 新しく追加されたファイルをUnityが認識");
            info.AppendLine("- 変更されたアセットのメタデータを更新");
            info.AppendLine("- インポート設定の変更を反映");

            return new StandardResponse
            {
                Success = true,
                FormattedOutput = info.ToString()
            };
        }

        /// <summary>プロジェクトを保存</summary>
        [McpServerTool, Description("現在のプロジェクトとアセットを保存")]
        public async ValueTask<object> SaveProject()
        {
            await UniTask.SwitchToMainThread();

            var startTime = DateTime.Now;
            AssetDatabase.SaveAssets();
            var duration = (DateTime.Now - startTime).TotalMilliseconds;

            var info = new System.Text.StringBuilder();
            info.AppendLine("=== プロジェクト保存 ===");
            info.AppendLine($"**実行時刻:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine($"**処理時間:** {duration:F2}ms");
            info.AppendLine();
            info.AppendLine("## ✅ 実行結果");
            info.AppendLine("💾 **プロジェクトとアセットの保存が完了しました**");
            info.AppendLine();
            info.AppendLine("## 💡 保存内容");
            info.AppendLine("- シーンの変更内容");
            info.AppendLine("- アセット設定の変更");
            info.AppendLine("- プロジェクト設定");
            info.AppendLine("- プレハブの変更");

            return new StandardResponse
            {
                Success = true,
                FormattedOutput = info.ToString()
            };
        }

        /// <summary>アセットを検索</summary>
        [McpServerTool, Description("指定したフィルターでアセットを検索（プロジェクトアセット優先表示）")]
        public async ValueTask<object> FindAssets(
            [Description("検索フィルター（ファイル名、タイプなど）")] string filter = "",
            [Description("検索するフォルダパス（空の場合は全体）")] string folder = "",
            [Description("最大結果数")] int maxResults = 50)
        {
            await UniTask.SwitchToMainThread();

            string[] searchFolders = string.IsNullOrEmpty(folder) ? null : new[] { folder };
            var guids = AssetDatabase.FindAssets(filter, searchFolders);

            var allResults = guids.Select(guid =>
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

                return new AssetSearchResult
                {
                    Guid = guid,
                    Path = path,
                    Name = asset ? asset.name : Path.GetFileNameWithoutExtension(path),
                    Type = asset ? asset.GetType().Name : "Unknown",
                    SizeBytes = File.Exists(path) ? new FileInfo(path).Length : 0,
                    LastModified = File.Exists(path) ? File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm:ss") : "Unknown"
                };
            }).ToList();

            // プロジェクトアセット（Assets/以下）を優先し、パッケージアセットは後回し
            var projectAssets = allResults.Where(r => r.Path.StartsWith("Assets/")).Take(maxResults).ToList();
            var packageAssets = allResults.Where(r => r.Path.StartsWith("Packages/")).Take(Math.Max(0, maxResults - projectAssets.Count)).ToList();

            var results = projectAssets.Concat(packageAssets).ToArray();

            // 読みやすい形式のサマリーを作成
            var summary = new System.Text.StringBuilder();
            summary.AppendLine($"=== アセット検索結果 ===");
            summary.AppendLine($"**検索条件:** {(string.IsNullOrEmpty(filter) ? "全てのアセット" : filter)}");
            summary.AppendLine($"**検索フォルダ:** {(string.IsNullOrEmpty(folder) ? "プロジェクト全体" : folder)}");
            summary.AppendLine($"**見つかった件数:** {guids.Length}件（表示: {results.Length}件）");
            summary.AppendLine();

            if (results.Length > 0)
            {
                // タイプ別の統計
                var typeGroups = results.GroupBy(r => r.Type).OrderByDescending(g => g.Count());
                summary.AppendLine("**ファイルタイプ別統計:**");
                foreach (var group in typeGroups.Take(5))
                {
                    summary.AppendLine($"- {group.Key}: {group.Count()}件");
                }

                summary.AppendLine();

                // プロジェクトアセットとパッケージアセットの分別表示
                if (projectAssets.Count > 0)
                {
                    summary.AppendLine("**プロジェクトアセット:**");
                    foreach (var asset in projectAssets.Take(10))
                    {
                        summary.AppendLine($"- **{asset.Name}** ({asset.Type}) - {asset.Path}");
                    }

                    if (projectAssets.Count > 10)
                    {
                        summary.AppendLine($"  ...他 {projectAssets.Count - 10}件");
                    }

                    summary.AppendLine();
                }

                if (packageAssets.Count > 0)
                {
                    summary.AppendLine("**パッケージアセット:**");
                    foreach (var asset in packageAssets.Take(5))
                    {
                        summary.AppendLine($"- **{asset.Name}** ({asset.Type}) - {asset.Path}");
                    }

                    if (packageAssets.Count > 5)
                    {
                        summary.AppendLine($"  ...他 {packageAssets.Count - 5}件");
                    }
                }
            }
            else
            {
                summary.AppendLine("**該当するアセットが見つかりませんでした。**");
            }

            return new StandardResponse
            {
                Success = true,
                FormattedOutput = summary.ToString()
            };
        }
    }
}
