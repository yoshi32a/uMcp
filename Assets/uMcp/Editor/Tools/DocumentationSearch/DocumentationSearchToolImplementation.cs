using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using uMCP.Editor.Core.Attributes;

namespace uMCP.Editor.Tools
{
    /// <summary>Unity ドキュメント検索ツール実装</summary>
    [McpServerToolType, Description("Unity公式ドキュメントを高速検索するためのツール（軽量インデックス対応）")]
    internal sealed class DocumentationSearchToolImplementation
    {
        static readonly string UnityInstallPath = GetUnityInstallPath();
        static readonly string DocumentationPath = GetDocumentationPath();

        static LightweightDocumentationIndex cachedLightIndex;

        /// <summary>Unity公式ドキュメントを高速検索（軽量インデックス使用）</summary>
        [McpServerTool, Description("指定されたクエリでUnity公式マニュアルを高速検索します")]
        public async ValueTask<DocumentationSearchResponse> SearchDocumentation(
            [Description("検索クエリ（英語推奨）")] string query,
            [Description("検索対象タイプ（現在はManualのみ対応）")]
            string searchType = "All",
            [Description("最大結果数（1-50）")] int maxResults = 10)
        {
            await UniTask.SwitchToMainThread();

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // パラメータ検証
                if (string.IsNullOrWhiteSpace(query))
                {
                    return new DocumentationSearchResponse
                    {
                        Success = false,
                        Query = query,
                        ErrorMessage = "検索クエリが空です"
                    };
                }

                maxResults = Mathf.Clamp(maxResults, 1, 50);

                // ドキュメントパスの確認
                if (!Directory.Exists(DocumentationPath))
                {
                    return new DocumentationSearchResponse
                    {
                        Success = false,
                        Query = query,
                        ErrorMessage = $"Unityドキュメントが見つかりません: {DocumentationPath}"
                    };
                }

                // 軽量インデックスを取得または構築
                await EnsureLightIndexAsync();

                if (cachedLightIndex == null)
                {
                    return new DocumentationSearchResponse
                    {
                        Success = false,
                        Query = query,
                        ErrorMessage = "ドキュメントインデックスの構築に失敗しました"
                    };
                }

                // 軽量インデックスベース検索実行
                var results = await SearchUsingLightIndexAsync(query, searchType, maxResults);

                stopwatch.Stop();

                var memoryUsage = GC.GetTotalMemory(false) / 1024 / 1024;

                return new DocumentationSearchResponse
                {
                    Success = true,
                    Query = query,
                    Results = results.Take(maxResults).ToList(),
                    TotalResults = results.Count,
                    DisplayedResults = Math.Min(results.Count, maxResults),
                    SearchTimeMs = stopwatch.ElapsedMilliseconds,
                    IndexInfo = $"軽量インデックス: {cachedLightIndex.TotalEntries}エントリ, {cachedLightIndex.KeywordIndex.Count}キーワード, メモリ使用量: {memoryUsage}MB"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new DocumentationSearchResponse
                {
                    Success = false,
                    Query = query,
                    ErrorMessage = $"検索中にエラーが発生しました: {ex.Message}",
                    SearchTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
        }

        /// <summary>軽量インデックス再構築ツール</summary>
        [McpServerTool, Description("並列処理でUnityマニュアルのインデックスを高速再構築します")]
        public async ValueTask<object> RebuildDocumentationIndex()
        {
            return await RebuildDocumentationIndexInternal();
        }


        /// <summary>インデックス再構築の内部実装</summary>
        async ValueTask<object> RebuildDocumentationIndexInternal()
        {
            await UniTask.SwitchToMainThread();

            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (!Directory.Exists(DocumentationPath))
                {
                    return new StandardResponse { Success = false, Error = $"Unityドキュメントが見つかりません: {DocumentationPath}" };
                }

                // 既存インデックスをクリア
                ParallelIndexBuilder.ClearLightIndex();
                cachedLightIndex = null;

                // 並列処理で新しい軽量インデックスを構築
                cachedLightIndex = await ParallelIndexBuilder.BuildOrUpdateIndexAsync(DocumentationPath);

                stopwatch.Stop();

                var memoryUsage = GC.GetTotalMemory(false) / 1024 / 1024;

                var info = new System.Text.StringBuilder();
                info.AppendLine("=== 並列軽量インデックス再構築完了 ===");
                info.AppendLine($"**処理モード:** Parallel");
                info.AppendLine($"**総エントリ数:** {cachedLightIndex.TotalEntries}");
                info.AppendLine($"**キーワード数:** {cachedLightIndex.KeywordIndex.Count}");
                info.AppendLine($"**構築時間:** {stopwatch.ElapsedMilliseconds}ms");
                info.AppendLine($"**メモリ使用量:** {memoryUsage}MB");
                info.AppendLine($"**Unity版本:** {cachedLightIndex.UnityVersion}");
                info.AppendLine($"**作成日時:** {cachedLightIndex.CreatedAt}");
                
                return new StandardResponse
                {
                    Success = true,
                    FormattedOutput = info.ToString()
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new StandardResponse
                {
                    Success = false,
                    Error = "並列軽量インデックス再構築エラー",
                    Message = $"{ex.Message} (構築時間: {stopwatch.ElapsedMilliseconds}ms)"
                };
            }
        }

        /// <summary>軽量インデックスが確実に存在することを保証</summary>
        async ValueTask EnsureLightIndexAsync()
        {
            if (cachedLightIndex != null)
                return;

            cachedLightIndex = await ParallelIndexBuilder.BuildOrUpdateIndexAsync(DocumentationPath);
        }

        /// <summary>軽量インデックスを使用した高速検索</summary>
        async ValueTask<List<DocumentationSearchResult>> SearchUsingLightIndexAsync(string query, string searchType, int maxResults)
        {
            var queryTerms = query.ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(term => term.Length >= 2)
                .ToArray();

            if (queryTerms.Length == 0)
                return new List<DocumentationSearchResult>();

            // 候補エントリのスコアを計算（軽量インデックスのみ使用）
            var candidateScores = new Dictionary<int, float>();

            foreach (var queryTerm in queryTerms)
            {
                // 完全一致
                if (cachedLightIndex.KeywordIndex.TryGetValue(queryTerm, out var exactMatches))
                {
                    foreach (var entryIndex in exactMatches)
                    {
                        candidateScores[entryIndex] = candidateScores.GetValueOrDefault(entryIndex, 0) + 5.0f;
                    }
                }

                // 部分一致
                var partialMatches = cachedLightIndex.KeywordIndex
                    .Where(kvp => kvp.Key.Contains(queryTerm))
                    .SelectMany(kvp => kvp.Value);

                foreach (var entryIndex in partialMatches)
                {
                    candidateScores[entryIndex] = candidateScores.GetValueOrDefault(entryIndex, 0) + 2.0f;
                }
            }

            // スコア順にソートして上位候補を取得
            var topCandidates = candidateScores
                .OrderByDescending(kvp => kvp.Value)
                .Take(maxResults * 2) // 余裕をもって取得
                .Select(kvp => kvp.Key)
                .ToList();

            if (topCandidates.Count == 0)
                return new List<DocumentationSearchResult>();

            // 詳細エントリをオンデマンドで読み込み
            var detailedEntries = await ParallelIndexBuilder.LoadDetailedEntriesAsync(topCandidates);

            var results = new List<DocumentationSearchResult>();

            for (int i = 0; i < detailedEntries.Count && results.Count < maxResults; i++)
            {
                var entry = detailedEntries[i];
                var candidateIndex = topCandidates[i];

                // 検索タイプでフィルタリング
                if (searchType != "All" && entry.DocumentType != searchType)
                    continue;

                // タイトルでの追加スコア計算
                var baseScore = candidateScores[candidateIndex];
                var titleScore = CalculateTitleScore(entry.Title, queryTerms);
                var finalScore = baseScore + titleScore;

                // コンテキスト付きスニペット生成
                var snippet = GenerateContextualSnippet(entry.ContentSnippet, queryTerms);

                results.Add(new DocumentationSearchResult
                {
                    FilePath = entry.FilePath,
                    Title = entry.Title,
                    DocumentType = entry.DocumentType,
                    ContentSnippet = snippet,
                    Score = finalScore,
                    RelativeUrl = entry.RelativeUrl
                });
            }

            return results.OrderByDescending(r => r.Score).ToList();
        }

        /// <summary>タイトルスコアを計算</summary>
        float CalculateTitleScore(string title, string[] queryTerms)
        {
            if (string.IsNullOrEmpty(title))
                return 0f;

            var titleLower = title.ToLowerInvariant();
            float score = 0f;

            foreach (var term in queryTerms)
            {
                if (titleLower == term)
                    score += 10f; // 完全一致
                else if (titleLower.Contains(term))
                    score += 3f; // 部分一致
            }

            return score;
        }

        /// <summary>コンテキスト付きスニペットを生成</summary>
        string GenerateContextualSnippet(string snippet, string[] queryTerms)
        {
            if (string.IsNullOrEmpty(snippet))
                return "";

            // 検索語が含まれている場合はそのまま返す
            var snippetLower = snippet.ToLowerInvariant();
            foreach (var term in queryTerms)
            {
                if (snippetLower.Contains(term))
                    return snippet;
            }

            // 含まれていない場合は先頭から返す
            return snippet.Length > 150 ? snippet.Substring(0, 150) + "..." : snippet;
        }

        /// <summary>Unityインストールパスを取得（Windows/Mac対応）</summary>
        static string GetUnityInstallPath()
        {
            var unityVersion = Application.unityVersion;
            
            // Mac環境の場合
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                var macPath = $"/Applications/Unity/Hub/Editor/{unityVersion}";
                if (Directory.Exists(macPath))
                    return macPath;
                    
                // フォールバック
                return "/Applications/Unity/Hub/Editor/6000.1.10f1";
            }
            
            // Windows環境
            var windowsPath = $@"C:\Program Files\Unity\Hub\Editor\{unityVersion}";
            if (Directory.Exists(windowsPath))
                return windowsPath;

            // フォールバック
            return @"C:\Program Files\Unity\Hub\Editor\6000.1.10f1";
        }

        /// <summary>ドキュメントパスを取得（Windows/Mac対応）</summary>
        static string GetDocumentationPath()
        {
            // Mac環境の場合
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                return Path.Combine(UnityInstallPath, "Unity.app", "Contents", "Documentation", "en");
            }
            
            // Windows環境
            return Path.Combine(UnityInstallPath, "Editor", "Data", "Documentation", "en");
        }
    }
}
