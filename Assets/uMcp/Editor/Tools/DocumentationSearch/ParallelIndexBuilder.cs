using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace uMCP.Editor.Tools
{
    /// <summary>並列処理対応インデックス構築クラス</summary>
    internal static class ParallelIndexBuilder
    {
        static readonly string IndexDirectory = Path.Combine("UserSettings", "document_index");
        static readonly string LightIndexPath = Path.Combine(IndexDirectory, "uMcp_ParallelLightIndex.json");
        static readonly string DetailIndexPath = Path.Combine(IndexDirectory, "detail");
        
        /// <summary>並列処理で軽量インデックスを構築または更新</summary>
        public static async ValueTask<LightweightDocumentationIndex> BuildOrUpdateIndexAsync(string documentationPath)
        {
            await UniTask.SwitchToMainThread();
            
            // 既存インデックスの読み込みを試みる
            var existingIndex = await LoadExistingIndexAsync();
            if (existingIndex != null && IsIndexValid(existingIndex, documentationPath))
            {
                UnityEngine.Debug.Log($"既存インデックスを使用: {existingIndex.TotalEntries}エントリ");
                return existingIndex;
            }
            
            UnityEngine.Debug.Log("並列処理で新しいインデックスを構築中...");
            
            var lightIndex = new LightweightDocumentationIndex
            {
                CreatedAt = DateTime.Now,
                UnityVersion = Application.unityVersion,
                DocumentationPath = documentationPath,
                DetailIndexPath = DetailIndexPath
            };
            
            // 検索対象ディレクトリ
            var searchDirs = new List<(string Path, string Type)>();
            
            var manualPath = Path.Combine(documentationPath, "Manual");
            if (Directory.Exists(manualPath))
                searchDirs.Add((manualPath, "Manual"));
            
            // ScriptReferenceは処理しない（Manualのみ）
            // var scriptRefPath = Path.Combine(documentationPath, "ScriptReference");  
            // if (Directory.Exists(scriptRefPath))
            //     searchDirs.Add((scriptRefPath, "ScriptReference"));
            
            if (searchDirs.Count == 0)
            {
                UnityEngine.Debug.LogError($"ドキュメントディレクトリが見つかりません: {documentationPath}");
                return lightIndex;
            }
            
            var stopwatch = Stopwatch.StartNew();
            var keywordIndex = new ConcurrentDictionary<string, ConcurrentBag<int>>();
            var entries = new ConcurrentBag<LightweightIndexEntry>();
            var entryCounter = 0;
            
            // 並列処理設定 - CPU使用率を抑えるため制限
            var maxConcurrency = Math.Min(Math.Max(Environment.ProcessorCount / 2, 2), 4); // 最大4並列、最小2並列
            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            
            UnityEngine.Debug.Log($"🚀 並列処理開始: 最大{maxConcurrency}並列");
            
            // インデックス保存ディレクトリ作成
            if (!Directory.Exists(IndexDirectory))
                Directory.CreateDirectory(IndexDirectory);
            if (!Directory.Exists(DetailIndexPath))
                Directory.CreateDirectory(DetailIndexPath);
            
            foreach (var (dirPath, docType) in searchDirs)
            {
                UnityEngine.Debug.Log($"📂 {docType}ディレクトリを処理中: {dirPath}");
                
                var htmlFiles = Directory.GetFiles(dirPath, "*.html", SearchOption.AllDirectories);
                UnityEngine.Debug.Log($"📄 {docType}: {htmlFiles.Length}ファイルを並列処理");
                
                // ファイルをバッチ処理（CPU負荷分散）
                const int batchSize = 100;
                for (int i = 0; i < htmlFiles.Length; i += batchSize)
                {
                    var batch = htmlFiles.Skip(i).Take(batchSize).ToArray();
                    var batchTasks = batch.Select(file => Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                    try
                    {
                        // CPU使用率を抑えるための小休止
                        await Task.Delay(10);
                        var content = await File.ReadAllTextAsync(file);
                        var title = ExtractTitle(content);
                        var textContent = ExtractTextFromHtml(content);
                        var keywords = GenerateKeywords(title, textContent);
                        
                        var currentIndex = Interlocked.Increment(ref entryCounter) - 1;
                        
                        // 軽量エントリを作成
                        var lightEntry = new LightweightIndexEntry
                        {
                            Title = title,
                            FilePath = file,
                            RelativeUrl = GetRelativePath(file, documentationPath),
                            DocumentType = docType,
                            TopKeywords = keywords.Take(5).ToList() // 上位5キーワード
                        };
                        
                        entries.Add(lightEntry);
                        
                        // キーワードインデックスを更新（スレッドセーフ）
                        foreach (var keyword in keywords)
                        {
                            keywordIndex.AddOrUpdate(keyword,
                                new ConcurrentBag<int> { currentIndex },
                                (key, existing) => { existing.Add(currentIndex); return existing; });
                        }
                        
                        // 詳細エントリを個別ファイルに保存
                        var detailedEntry = new DetailedIndexEntry
                        {
                            Title = title,
                            FilePath = file,
                            RelativeUrl = GetRelativePath(file, documentationPath),
                            DocumentType = docType,
                            ContentSnippet = textContent.Length > 1000 ? textContent.Substring(0, 1000) + "..." : textContent,
                            AllKeywords = keywords
                        };
                        
                        var entryFilePath = Path.Combine(DetailIndexPath, $"{currentIndex}_{Guid.NewGuid():N}.json");
                        var entryJson = JsonSerializer.Serialize(detailedEntry, new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        });
                        await File.WriteAllTextAsync(entryFilePath, entryJson);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"ファイル処理エラー: {file} - {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                    }));
                    
                    await Task.WhenAll(batchTasks);
                    
                    // バッチ間で小休止（CPU負荷を分散）
                    if (i + batchSize < htmlFiles.Length)
                    {
                        await Task.Delay(50);
                        UnityEngine.Debug.Log($"  進捗: {Math.Min(i + batchSize, htmlFiles.Length)}/{htmlFiles.Length} ファイル処理済み");
                    }
                }
                UnityEngine.Debug.Log($"✅ {docType}処理完了: {htmlFiles.Length}ファイル");
            }
            
            // ConcurrentBagからListに変換
            var sortedEntries = entries.OrderBy(e => e.RelativeUrl).ToList();
            
            // キーワードインデックスをDictionaryに変換
            var finalKeywordIndex = new Dictionary<string, List<int>>();
            foreach (var kvp in keywordIndex)
            {
                finalKeywordIndex[kvp.Key] = kvp.Value.ToList();
            }
            
            lightIndex.KeywordIndex = finalKeywordIndex;
            lightIndex.TotalEntries = sortedEntries.Count;
            
            // インデックスを保存
            var indexJson = JsonSerializer.Serialize(lightIndex, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            
            await File.WriteAllTextAsync(LightIndexPath, indexJson);
            
            stopwatch.Stop();
            UnityEngine.Debug.Log($"🎉 並列インデックス構築完了: {lightIndex.TotalEntries}エントリ, {lightIndex.KeywordIndex.Count}キーワード ({stopwatch.ElapsedMilliseconds}ms)");
            
            return lightIndex;
        }
        
        /// <summary>既存の並列インデックスをクリア</summary>
        public static void ClearLightIndex()
        {
            try
            {
                if (File.Exists(LightIndexPath))
                {
                    File.Delete(LightIndexPath);
                    UnityEngine.Debug.Log("既存の並列軽量インデックスを削除しました");
                }
                
                if (Directory.Exists(DetailIndexPath))
                {
                    var detailFiles = Directory.GetFiles(DetailIndexPath, "*.json");
                    foreach (var file in detailFiles)
                    {
                        File.Delete(file);
                    }
                    UnityEngine.Debug.Log($"詳細インデックスファイル {detailFiles.Length}個を削除しました");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"インデックスクリア中にエラー: {ex.Message}");
            }
        }

    /// <summary>詳細エントリをオンデマンドで読み込み</summary>
    public static async ValueTask<List<DetailedIndexEntry>> LoadDetailedEntriesAsync(List<int> entryIndices)
    {
        await UniTask.SwitchToMainThread();
        
        var detailedEntries = new List<DetailedIndexEntry>();
        if (!Directory.Exists(DetailIndexPath))
            return detailedEntries;
        
        foreach (var index in entryIndices)
        {
            try 
            {
                // インデックスに対応するファイルを検索（GUID付き）
                var files = Directory.GetFiles(DetailIndexPath, $"{index}_*.json");
                if (files.Length > 0)
                {
                    var entryFilePath = files[0];
                    var json = await File.ReadAllTextAsync(entryFilePath);
                    var entry = JsonSerializer.Deserialize<DetailedIndexEntry>(json);
                    if (entry != null)
                        detailedEntries.Add(entry);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"詳細エントリ読み込みエラー {index}: {ex.Message}");
            }
        }
        
        return detailedEntries;
    }

        /// <summary>既存インデックスを読み込む</summary>
        static async ValueTask<LightweightDocumentationIndex> LoadExistingIndexAsync()
        {
            try
            {
                if (!File.Exists(LightIndexPath))
                    return null;
                
                var json = await File.ReadAllTextAsync(LightIndexPath);
                var index = JsonSerializer.Deserialize<LightweightDocumentationIndex>(json);
                return index;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"インデックス読み込みエラー: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>インデックスの有効性をチェック</summary>
        static bool IsIndexValid(LightweightDocumentationIndex index, string documentationPath)
        {
            if (index == null) return false;
            
            // パス一致確認
            if (index.DocumentationPath != documentationPath) return false;
            
            // Unityバージョン一致確認
            if (index.UnityVersion != Application.unityVersion) return false;
            
            // 7日経過チェックを削除 - 一度作成したら永続的に使用
            // if (DateTime.Now - index.CreatedAt > TimeSpan.FromDays(7)) return false;
            
            return true;
        }
        
        /// <summary>HTMLからタイトル抽出</summary>
        static string ExtractTitle(string htmlContent)
        {
            var titleMatch = System.Text.RegularExpressions.Regex.Match(htmlContent, @"<title[^>]*>(.*?)</title>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (titleMatch.Success)
            {
                var title = System.Net.WebUtility.HtmlDecode(titleMatch.Groups[1].Value).Trim();
                title = System.Text.RegularExpressions.Regex.Replace(title, @"^Unity\s*-\s*(Manual|Scripting API):\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return title;
            }
            return "Untitled";
        }
        
        /// <summary>HTMLからテキスト抽出</summary>
        static string ExtractTextFromHtml(string htmlContent)
        {
            var text = System.Text.RegularExpressions.Regex.Replace(htmlContent, @"<script[^>]*>.*?</script>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<style[^>]*>.*?</style>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]*>", " ");
            text = System.Net.WebUtility.HtmlDecode(text);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
            return text.Trim();
        }
        
        /// <summary>キーワード生成</summary>
        static List<string> GenerateKeywords(string title, string content)
        {
            var keywords = new HashSet<string>();
            
            // タイトルから全単語を抽出（高重要度）
            var titleWords = ExtractWords(title.ToLowerInvariant());
            foreach (var word in titleWords.Where(w => w.Length >= 2))
                keywords.Add(word);
            
            // コンテンツから頻度ベース抽出
            var wordFreq = new Dictionary<string, int>();
            var contentWords = ExtractWords(content.ToLowerInvariant());
            
            foreach (var word in contentWords.Where(w => w.Length >= 3 && !IsStopWord(w)))
            {
                wordFreq[word] = wordFreq.GetValueOrDefault(word, 0) + 1;
            }
            
            // 頻出単語を選択（2回以上出現、上位キーワード制限なし）
            var topWords = wordFreq
                .Where(kvp => kvp.Value >= 2)
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key);
            
            keywords.UnionWith(topWords);
            return keywords.ToList();
        }
        
        /// <summary>単語抽出</summary>
        static IEnumerable<string> ExtractWords(string text)
        {
            return System.Text.RegularExpressions.Regex.Matches(text, @"\b[a-zA-Z][a-zA-Z0-9]*\b")
                .Select(m => m.Value);
        }
        
        /// <summary>ストップワード判定</summary>
        static bool IsStopWord(string word)
        {
            var stopWords = new HashSet<string> { "the", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "is", "are", "was", "were", "be", "been", "have", "has", "had", "do", "does", "did", "will", "would", "could", "should", "may", "might", "can", "this", "that", "these", "those", "i", "you", "he", "she", "it", "we", "they", "me", "him", "her", "us", "them", "my", "your", "his", "her", "its", "our", "their", "a", "an", "as", "if", "when", "where", "why", "how", "what", "which", "who", "whom", "whose", "all", "any", "each", "every", "no", "not", "only", "own", "same", "so", "than", "too", "very", "just", "now" };
            return stopWords.Contains(word);
        }
        
        /// <summary>相対パス取得</summary>
        static string GetRelativePath(string filePath, string basePath)
        {
            var uri1 = new Uri(basePath + Path.DirectorySeparatorChar);
            var uri2 = new Uri(filePath);
            return Uri.UnescapeDataString(uri1.MakeRelativeUri(uri2).ToString().Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
