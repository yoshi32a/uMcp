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

            return new
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

            return new
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

            return new
            {
                Success = true,
                FormattedOutput = summary.ToString()
            };
        }

        /// <summary>アセットの詳細情報を取得</summary>
        [McpServerTool, Description("指定したパスのアセットの詳細情報を読みやすい形式で取得")]
        public async ValueTask<object> GetAssetInfo([Description("アセットのパス")] string assetPath)
        {
            await UniTask.SwitchToMainThread();

            if (string.IsNullOrEmpty(assetPath))
            {
                return new ErrorResponse
                {
                    Success = false,
                    Error = "Asset path is required"
                };
            }

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (!asset)
            {
                return new ErrorResponse
                {
                    Success = false,
                    Error = $"Asset not found at path: {assetPath}"
                };
            }

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var importer = AssetImporter.GetAtPath(assetPath);
            var dependencies = AssetDatabase.GetDependencies(assetPath, false);

            var info = new System.Text.StringBuilder();
            info.AppendLine($"=== アセット詳細: {asset.name} ===");
            info.AppendLine($"**パス:** {assetPath}");
            info.AppendLine($"**GUID:** {guid}");
            info.AppendLine();

            // 基本情報
            info.AppendLine("## 📋 基本情報");
            var icon = asset.GetType().Name switch
            {
                "SceneAsset" => "🎬",
                "GameObject" => "🎮", 
                "Material" => "🎨",
                "Texture2D" => "🖼️",
                "AudioClip" => "🔊",
                "MonoScript" => "📜",
                "Shader" => "✨",
                "Mesh" => "📐",
                _ => "📄"
            };
            
            info.AppendLine($"{icon} **{asset.name}** ({asset.GetType().Name})");
            info.AppendLine($"**サイズ:** {FormatFileSize(File.Exists(assetPath) ? new FileInfo(assetPath).Length : 0)}");
            info.AppendLine($"**最終更新:** {(File.Exists(assetPath) ? File.GetLastWriteTime(assetPath).ToString("yyyy-MM-dd HH:mm:ss") : "不明")}");
            info.AppendLine($"**インポーター:** {(importer ? importer.GetType().Name : "なし")}");
            info.AppendLine();

            // ラベル
            var labels = AssetDatabase.GetLabels(asset);
            if (labels.Length > 0)
            {
                info.AppendLine("## 🏷️ ラベル");
                foreach (var label in labels)
                {
                    info.AppendLine($"- {label}");
                }
                info.AppendLine();
            }

            // 依存関係
            if (dependencies.Length > 0)
            {
                info.AppendLine($"## 🔗 依存関係 ({dependencies.Length}件)");
                foreach (var dep in dependencies.Take(10))
                {
                    var depAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dep);
                    var depIcon = depAsset?.GetType().Name switch
                    {
                        "Material" => "🎨",
                        "Texture2D" => "🖼️",
                        "MonoScript" => "📜",
                        "Shader" => "✨",
                        _ => "📄"
                    };
                    info.AppendLine($"{depIcon} **{Path.GetFileNameWithoutExtension(dep)}** - {dep}");
                }
                if (dependencies.Length > 10)
                {
                    info.AppendLine($"   ...他 {dependencies.Length - 10}件");
                }
            }

            return new
            {
                Success = true,
                FormattedOutput = info.ToString()
            };
        }

        /// <summary>アセットを再インポート</summary>
        [McpServerTool, Description("指定したアセットを強制再インポート")]
        public async ValueTask<object> ReimportAsset([Description("再インポートするアセットのパス")] string assetPath)
        {
            await UniTask.SwitchToMainThread();

            if (string.IsNullOrEmpty(assetPath))
            {
                return new ErrorResponse
                {
                    Success = false,
                    Error = "Asset path is required"
                };
            }

            if (!AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath))
            {
                return new ErrorResponse
                {
                    Success = false,
                    Error = $"Asset not found at path: {assetPath}"
                };
            }

            var startTime = DateTime.Now;
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var duration = (DateTime.Now - startTime).TotalMilliseconds;

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            var info = new System.Text.StringBuilder();
            info.AppendLine($"=== アセット再インポート: {(asset ? asset.name : Path.GetFileNameWithoutExtension(assetPath))} ===");
            info.AppendLine($"**パス:** {assetPath}");
            info.AppendLine($"**実行時刻:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine($"**処理時間:** {duration:F2}ms");
            info.AppendLine();
            
            var icon = asset?.GetType().Name switch
            {
                "SceneAsset" => "🎬",
                "Material" => "🎨",
                "Texture2D" => "🖼️",
                "AudioClip" => "🔊",
                "MonoScript" => "📜",
                "Shader" => "✨",
                "Mesh" => "📐",
                _ => "📄"
            };
            
            info.AppendLine("## ✅ 実行結果");
            info.AppendLine($"{icon} **アセットの再インポートが正常に完了しました**");
            info.AppendLine();
            info.AppendLine("## 💡 効果");
            info.AppendLine("- インポート設定の強制再適用");
            info.AppendLine("- メタデータの再生成");
            info.AppendLine("- 依存関係の再構築");
            info.AppendLine("- キャッシュのクリア");

            return new
            {
                Success = true,
                FormattedOutput = info.ToString()
            };
        }

        /// <summary>ファイルサイズを読みやすい形式にフォーマット</summary>
        string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";
            
            var units = new[] { "B", "KB", "MB", "GB" };
            var unitIndex = 0;
            var size = (double)bytes;
            
            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }
            
            return $"{size:F1} {units[unitIndex]}";
        }
    }
}