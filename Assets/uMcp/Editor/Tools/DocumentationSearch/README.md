# Unity Documentation Search Tool システム解説

## 概要

Unity Documentation Search Tool は、Unity公式ドキュメント（Manual/ScriptReference）をMCP経由で検索可能にするツールです。AIアシスタント（Claude、GitHub Copilot等）がUnity公式情報を参照して、正確で最新の回答を提供できるようになります。

## システムアーキテクチャ

### 🏗️ 全体構成

```
[AIアシスタント] 
    ↓ MCP Protocol
[Unity MCP Server] 
    ↓ search_documentation ツール
[DocumentationSearchToolImplementation]
    ↓ HTML解析・検索
[Unity公式ドキュメント]
    C:\Program Files\Unity\Hub\Editor\{version}\Editor\Data\Documentation\en\
    ├── Manual\          (使い方・概念)
    └── ScriptReference\ (C# API リファレンス)
```

### 📁 ファイル構成

```
Assets/uMcp/Editor/Tools/DocumentationSearch/
├── DocumentationSearchTool.cs                    # ScriptableObject ツール定義
├── DocumentationSearchToolImplementation.cs     # メイン検索ロジック
├── DocumentationSearchResponse.cs               # レスポンス形式
├── DocumentationSearchResult.cs                 # 個別検索結果
└── README.md                                    # このファイル
```

### 🔧 主要コンポーネント

#### 1. DocumentationSearchTool (ScriptableObject)
- **役割**: Unity Editor内でのツール設定・管理
- **継承**: `UMcpToolBuilder` 
- **DI登録**: `ServiceCollectionBuilder` でImplementationを登録
- **CreateAssetMenu**: `Assets > Create > uMCP > Tools > Documentation Search Tool`

#### 2. DocumentationSearchToolImplementation (Core Logic)
- **属性**: `[McpServerToolType]` でMCPツールとして自動登録
- **メソッド**: `SearchDocumentation` が `[McpServerTool]` でMCP API化
- **パラメータ**: 
  - `query`: 検索クエリ（英語推奨）
  - `searchType`: All/Manual/ScriptReference
  - `maxResults`: 最大結果数（1-50）

#### 3. DocumentationSearchResponse (Response Model)
- **FormattedOutput**: 読みやすい形式での結果表示
- **メタデータ**: 検索時間、総結果数、表示数
- **エラーハンドリング**: エラーメッセージと成功フラグ

#### 4. DocumentationSearchResult (Result Item)
- **ファイル情報**: パス、タイトル、ドキュメントタイプ
- **検索結果**: スコア、コンテンツスニペット
- **URL**: 相対パスでの文書参照

## 検索システムの詳細

### 🔍 検索フロー

1. **パラメータ検証**: クエリの有効性、結果数制限
2. **パス確認**: Unityドキュメントフォルダの存在チェック
3. **ディレクトリ走査**: Manual/ScriptReferenceのHTMLファイル列挙
4. **ファイル解析**: 各HTMLファイルを並列処理で分析
5. **スコアリング**: 検索語との一致度計算
6. **結果整理**: スコア順ソート、上位結果を返却

### 📊 スコアリングアルゴリズム

```csharp
float CalculateSearchScore(string title, string content, string[] searchTerms)
{
    foreach (var term in searchTerms)
    {
        // タイトル完全一致: 10点
        if (titleLower == term) score += 10f;
        
        // タイトル部分一致: 5点
        else if (titleLower.Contains(term)) score += 5f;
        
        // コンテンツ内出現: 0.5点/回
        var matches = Regex.Matches(contentLower, term).Count;
        score += matches * 0.5f;
    }
    
    // 0-1範囲に正規化
    return Math.Min(score / (searchTerms.Length * 10f), 1f);
}
```

### 🛠️ HTML処理パイプライン

#### 1. テキスト抽出
```csharp
// HTMLタグ除去
var text = Regex.Replace(html, "<[^>]*>", " ");
// HTMLエンティティデコード
text = System.Net.WebUtility.HtmlDecode(text);
// 余分な空白削除
text = Regex.Replace(text, @"\s+", " ");
```

#### 2. タイトル抽出
```csharp
// <title>タグから抽出
var titleMatch = Regex.Match(html, @"<title[^>]*>(.*?)</title>");
// "Unity - Manual: " プレフィックス除去
title = Regex.Replace(title, @"^Unity\s*-\s*(Manual|Scripting API):\s*", "");
```

#### 3. スニペット生成
```csharp
// 検索語が含まれる箇所を特定
var index = contentLower.IndexOf(searchTerm);
// 前後100文字を取得（最大300文字）
int start = Math.Max(0, index - 100);
int length = Math.Min(300, content.Length - start);
var snippet = content.Substring(start, length);
```

### 🚀 高速化技術：インデックス化システム

#### 1. **事前インデックス構築による劇的な高速化**

**従来の問題点：**
- HTMLファイルを毎回シーケンシャル処理（52秒）
- 5000+ファイルの逐次読み込みによるボトルネック
- リアルタイム正規表現処理による重い負荷

**新しいアプローチ：**
```csharp
// 1. 事前インデックス構築（初回のみ）
cachedIndex = await DocumentationIndexBuilder.BuildOrUpdateIndexAsync(DocumentationPath);

// 2. キーワードマップによるO(1)検索
if (cachedIndex.KeywordIndex.TryGetValue(queryTerm, out var exactMatches))
{
    foreach (var entryIndex in exactMatches)
    {
        candidateScores[entryIndex] += 5.0f; // 完全一致
    }
}

// 3. スコアベースランキング
var sortedCandidates = candidateScores
    .OrderByDescending(kvp => kvp.Value)
    .Take(maxResults * 2);
```

#### 2. **インデックス構造とキャッシング**

```csharp
public class DocumentationIndex
{
    // メタデータ
    public string Version { get; set; } = "1.0";
    public DateTime CreatedAt { get; set; }
    public string UnityVersion { get; set; }
    
    // インデックスエントリ（全文書情報）
    public List<DocumentationIndexEntry> Entries { get; set; }
    
    // 高速検索用キーワードマップ（O(1)アクセス）
    public Dictionary<string, List<int>> KeywordIndex { get; set; }
}
```

**インデックスファイル保存場所:**
```
C:\Users\{user}\AppData\LocalLow\DefaultCompany\UnityMcpTest\uMcp_DocumentationIndex.json
```

#### 3. **インテリジェントキーワード抽出**

```csharp
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
    
    // 頻度上位20単語を選択（2回以上出現）
    var topWords = wordFreq
        .Where(kvp => kvp.Value >= 2)
        .OrderByDescending(kvp => kvp.Value)
        .Take(20)
        .Select(kvp => kvp.Key);
    
    keywords.UnionWith(topWords);
    return keywords.ToList();
}
```

#### 4. **自動インデックス管理**

**インデックス有効性チェック:**
```csharp
static bool IsIndexValid(DocumentationIndex index, string documentationPath)
{
    if (index == null) return false;
    
    // パス一致確認
    if (index.DocumentationPath != documentationPath) return false;
    
    // Unityバージョン一致確認
    if (index.UnityVersion != Application.unityVersion) return false;
    
    // 7日以内作成確認
    if (DateTime.Now - index.CreatedAt > TimeSpan.FromDays(7)) return false;
    
    return true;
}
```

**自動更新条件:**
- 🔄 Unityバージョン変更時
- 📅 インデックス作成から7日経過時
- 🗂️ ドキュメントパス変更時
- 🚫 インデックスファイル不存在時

#### 5. **パフォーマンス比較**

| 項目 | 従来方式 | インデックス方式 |
|------|----------|------------------|
| **初回実行** | 52秒 | 2-3分（インデックス構築）|
| **2回目以降** | 52秒 | **< 1秒** ⚡ |
| **メモリ使用量** | 動的（大きな負荷）| 固定（軽量）|
| **CPU使用率** | 高（リアルタイム処理）| 低（インデックス参照）|
| **スケーラビリティ** | 悪い（O(n)）| 良い（O(1)）|

#### 6. **高度なスコアリングシステム**

```csharp
// マルチレベルスコアリング
foreach (var queryTerm in queryTerms)
{
    // 完全一致キーワード: 5.0点
    if (cachedIndex.KeywordIndex.TryGetValue(queryTerm, out var exactMatches))
        foreach (var idx in exactMatches)
            candidateScores[idx] += 5.0f;
    
    // 部分一致キーワード: 2.0点
    var partialMatches = cachedIndex.KeywordIndex
        .Where(kvp => kvp.Key.Contains(queryTerm))
        .SelectMany(kvp => kvp.Value);
    foreach (var idx in partialMatches)
        candidateScores[idx] += 2.0f;
}

// タイトル特別スコア: 最大10.0点
var titleScore = CalculateTitleScore(entry.Title, queryTerms);
var finalScore = keywordScore + titleScore;
```

#### 7. **新しいMCPツール**

**search_documentation** （既存改良）
- インデックスベース高速検索
- 従来の52秒 → **1秒以内**

**rebuild_documentation_index** （新規追加）
- 強制インデックス再構築
- 初回セットアップ用
- Unity バージョン更新後の更新用

## MCP統合システム

### 🔗 属性ベース自動登録

```csharp
[McpServerToolType, Description("Unity公式ドキュメントを検索するためのツール")]
internal sealed class DocumentationSearchToolImplementation
{
    [McpServerTool, Description("指定されたクエリでUnity公式ドキュメントを検索")]
    public async ValueTask<DocumentationSearchResponse> SearchDocumentation(...)
}
```

### 📡 JSON-RPC 2.0 対応

MCPプロトコル経由で以下のように呼び出し可能：

```json
{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
        "name": "search_documentation",
        "arguments": {
            "query": "NavMesh pathfinding",
            "searchType": "Manual",
            "maxResults": 5
        }
    }
}
```

### 🎯 FormattedOutput対応

結果は読みやすい形式で自動整理：

```
=== Unity ドキュメント検索結果 ===
**検索クエリ:** NavMesh pathfinding
**検索時間:** 245ms
**結果:** 5件表示（全12件）

📖 **Navigation and Pathfinding** (スコア: 0.85)
   **タイプ:** Manual
   **パス:** Manual/NavMeshPathfinding.html
   **内容:** Unity's NavMesh system provides automatic pathfinding...
```

## 使用方法

### 1. **初回セットアップ（インデックス構築）**

```bash
# 方法1: 初回検索時の自動構築
claude chat
> Unity の Vector3 について教えて
# → 初回は2-3分かけてインデックス構築
# → 以降は高速検索が可能

# 方法2: 手動インデックス構築
# MCP経由で rebuild_documentation_index を実行
```

**インデックス構築プロセス:**
```
📊 インデックス構築中...
├── Manual: 3205ファイルを処理中...
├── ScriptReference: 2847ファイルを処理中...
├── キーワード抽出: 145,892キーワード
└── インデックス保存: 完了 (2分34秒)
```

### 2. **高速検索の利用**

```bash
# Claude CLI経由での高速検索
claude chat
> Unity の NavMesh pathfinding について詳しく教えて

# AIが自動的に search_documentation を実行
# → インデックスから高速検索 (< 1秒)
# → Unity公式ドキュメントから正確な情報取得
# → 最新の公式情報に基づく回答を生成
```

**検索結果例:**
```
=== Unity ドキュメント検索結果 ===
**検索クエリ:** NavMesh pathfinding
**検索時間:** 245ms
**インデックス: 6052エントリ, 145892キーワード**
**結果:** 5件表示（全12件）

📖 **Navigation and Pathfinding** (スコア: 0.85)
   **タイプ:** Manual
   **パス:** Manual/nav-NavigationSystem.html
   **内容:** Unity's navigation system allows you to create characters that can intelligently move around the game world...
```

### 3. **MCP ツールAPI**

**search_documentation** (高速検索)
```json
{
    "name": "search_documentation",
    "arguments": {
        "query": "Vector3 operations",
        "searchType": "ScriptReference",
        "maxResults": 10
    }
}
```

**rebuild_documentation_index** (インデックス再構築)
```json
{
    "name": "rebuild_documentation_index",
    "arguments": {}
}
```

### 4. **直接API呼び出し**

```csharp
// 高速検索
var response = await searchTool.SearchDocumentation(
    query: "Vector3 operations",
    searchType: "ScriptReference", 
    maxResults: 10
);

// インデックス再構築
var rebuildResult = await searchTool.RebuildDocumentationIndex();
Console.WriteLine($"構築完了: {rebuildResult.EntriesCount}エントリ");
```

## 技術仕様

### システム要件
- **Unity**: 2022.3 LTS以上
- **依存関係**: UniTask 2.3.3+
- **プラットフォーム**: Windows（Unityドキュメントパス対応）
- **ドキュメント**: Unity公式ドキュメントのローカルインストール

### パフォーマンス指標

#### **インデックス構築時（初回のみ）**
- **構築時間**: 2-3分（Unity 6000.1.10f1）
- **処理ファイル数**: ~6000ファイル（Manual: 3205 + ScriptReference: 2847）
- **生成キーワード数**: ~150,000キーワード
- **インデックスファイルサイズ**: 15-20MB
- **メモリ使用**: 構築時最大500MB

#### **検索実行時（2回目以降）**
- **検索速度**: **< 1秒** ⚡（従来52秒 → 50倍高速化）
- **メモリ使用**: 20-30MB（インデックスキャッシュ）
- **CPU使用率**: 低負荷（O(1)検索）
- **同時実行**: 完全非同期、UI阻害なし

#### **スケーラビリティ**
- **時間計算量**: O(1) キーワード検索 + O(k log k) スコアソート
- **空間計算量**: O(n) インデックスサイズ（n=文書数）
- **インデックス更新**: 7日間隔の自動更新

### 制限事項
- **言語**: 英語ドキュメントのみ対応
- **プラットフォーム**: Windows対応（Unity標準パス）
- **検索精度**: キーワードベース（将来的にRAG統合予定）
- **初回実行**: インデックス構築により初回のみ時間要

## 拡張ポイント

### 将来的な改善案

1. **RAG統合**: ベクトル検索による意味的検索の高精度化
2. **多言語対応**: 日本語ドキュメント対応
3. **クロスプラットフォーム**: macOS/Linux対応
4. ✅ **インデックス化**: **完了** - 50倍高速化を実現
5. **カスタムドキュメント**: ユーザー独自ドキュメント対応
6. **検索履歴**: よく検索される内容の学習・提案
7. **増分インデックス**: 変更ファイルのみ更新による高速化
8. **分散インデックス**: 複数Unityバージョン対応

### カスタマイズ方法

```csharp
// 独自スコアリングアルゴリズム
private float CustomCalculateScore(string title, string content, string[] terms)
{
    // カスタムロジック実装
}

// 追加検索ディレクトリ
private readonly string[] CustomDocPaths = {
    "path/to/custom/docs",
    "path/to/project/docs"
};
```

---

**Unity Documentation Search Tool** により、Unity MCP Server は AI アシスタントが Unity 公式情報を参照可能な、より強力なツールセットとなりました。