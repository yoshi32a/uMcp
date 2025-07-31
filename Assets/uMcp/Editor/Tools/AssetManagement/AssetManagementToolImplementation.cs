using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using uMCP.Editor.Core.Attributes;
using UnityEditor;
using UnityEngine;

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

            var startTime = System.DateTime.Now;
            
            // シンプルにリフレッシュのみ
            AssetDatabase.Refresh();
            
            var duration = (System.DateTime.Now - startTime).TotalMilliseconds;

            return new AssetOperationResponse
            {
                Success = true,
                Message = "Asset database refreshed successfully",
                DurationMs = duration,
                Timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        /// <summary>プロジェクトを保存</summary>
        [McpServerTool, Description("現在のプロジェクトとアセットを保存")]
        public async ValueTask<object> SaveProject()
        {
            await UniTask.SwitchToMainThread();

            var startTime = System.DateTime.Now;
            AssetDatabase.SaveAssets();
            var duration = (System.DateTime.Now - startTime).TotalMilliseconds;

            return new AssetOperationResponse
            {
                Success = true,
                Message = "Project saved successfully",
                DurationMs = duration,
                Timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        /// <summary>アセットを検索</summary>
        [McpServerTool, Description("指定したフィルターでアセットを検索")]
        public async ValueTask<object> FindAssets(
            [Description("検索フィルター（ファイル名、タイプなど）")] string filter = "",
            [Description("検索するフォルダパス（空の場合は全体）")] string folder = "",
            [Description("最大結果数")] int maxResults = 50)
        {
            await UniTask.SwitchToMainThread();

            string[] searchFolders = string.IsNullOrEmpty(folder) ? null : new[] { folder };
            var guids = AssetDatabase.FindAssets(filter, searchFolders);

            var results = guids.Take(maxResults).Select(guid =>
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);

                return new AssetSearchResult
                {
                    Guid = guid,
                    Path = path,
                    Name = asset ? asset.name : Path.GetFileNameWithoutExtension(path),
                    Type = asset ? asset.GetType().Name : "Unknown",
                    SizeBytes = File.Exists(path) ? new FileInfo(path).Length : 0,
                    LastModified = File.Exists(path) ? File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm:ss") : "Unknown"
                };
            }).ToArray();

            return new AssetSearchResponse
            {
                Success = true,
                SearchFilter = filter,
                SearchFolder = folder,
                TotalFound = guids.Length,
                ReturnedCount = results.Length,
                Results = results
            };
        }

        /// <summary>アセットの詳細情報を取得</summary>
        [McpServerTool, Description("指定したパスのアセットの詳細情報を取得")]
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

            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
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

            return new AssetInfoResponse
            {
                Success = true,
                AssetInfo = new AssetInfo
                {
                    Name = asset.name,
                    Path = assetPath,
                    Guid = guid,
                    Type = asset.GetType().Name,
                    SizeBytes = File.Exists(assetPath) ? new FileInfo(assetPath).Length : 0,
                    LastModified = File.Exists(assetPath) ? File.GetLastWriteTime(assetPath).ToString("yyyy-MM-dd HH:mm:ss") : "Unknown",
                    ImporterType = importer ? importer.GetType().Name : "None",
                    Labels = AssetDatabase.GetLabels(asset),
                    Dependencies = dependencies,
                    DependencyCount = dependencies.Length
                }
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

            if (!AssetDatabase.LoadAssetAtPath<Object>(assetPath))
            {
                return new ErrorResponse
                {
                    Success = false,
                    Error = $"Asset not found at path: {assetPath}"
                };
            }

            var startTime = System.DateTime.Now;
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var duration = (System.DateTime.Now - startTime).TotalMilliseconds;

            return new AssetOperationResponse
            {
                Success = true,
                Message = $"Asset reimported successfully: {assetPath}",
                DurationMs = duration,
                Timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
    }
}