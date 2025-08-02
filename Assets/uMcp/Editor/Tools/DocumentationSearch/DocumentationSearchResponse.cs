using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uMCP.Editor.Tools
{
    /// <summary>ドキュメント検索レスポンス</summary>
    [Serializable]
    public class DocumentationSearchResponse
    {
        /// <summary>操作が成功したかどうか</summary>
        public bool Success { get; set; }
        
        /// <summary>検索クエリ</summary>
        public string Query { get; set; }
        
        /// <summary>検索結果リスト</summary>
        public List<DocumentationSearchResult> Results { get; set; } = new();
        
        /// <summary>総検索結果数</summary>
        public int TotalResults { get; set; }
        
        /// <summary>表示した結果数</summary>
        public int DisplayedResults { get; set; }
        
        /// <summary>検索に要した時間（ミリ秒）</summary>
        public long SearchTimeMs { get; set; }
        
        /// <summary>エラーメッセージ</summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>インデックス情報</summary>
        public string IndexInfo { get; set; }
        
        /// <summary>フォーマットされた出力</summary>
        public string FormattedOutput
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== Unity ドキュメント検索結果 ===");
                sb.AppendLine($"**検索クエリ:** {Query}");
                sb.AppendLine($"**検索時間:** {SearchTimeMs}ms");
                
                if (!string.IsNullOrEmpty(IndexInfo))
                {
                    sb.AppendLine($"**{IndexInfo}**");
                }
                sb.AppendLine($"**結果:** {DisplayedResults}件表示（全{TotalResults}件）");
                sb.AppendLine();
                
                if (!Success)
                {
                    sb.AppendLine($"❌ **エラー:** {ErrorMessage}");
                    return sb.ToString();
                }
                
                if (!Results.Any())
                {
                    sb.AppendLine("🔍 **該当する文書が見つかりませんでした**");
                    sb.AppendLine();
                    sb.AppendLine("**検索のヒント:**");
                    sb.AppendLine("- より一般的なキーワードを試してください");
                    sb.AppendLine("- スペルを確認してください");
                    sb.AppendLine("- 英語のキーワードを試してください");
                    return sb.ToString();
                }
                
                foreach (var result in Results.Take(10))
                {
                    var docTypeIcon = result.DocumentType == "Manual" ? "📖" : "🔧";
                    sb.AppendLine($"{docTypeIcon} **{result.Title}** (スコア: {result.Score:F2})");
                    sb.AppendLine($"   **タイプ:** {result.DocumentType}");
                    sb.AppendLine($"   **パス:** {result.RelativeUrl}");
                    
                    if (!string.IsNullOrEmpty(result.ContentSnippet))
                    {
                        var snippet = result.ContentSnippet.Length > 150 
                            ? result.ContentSnippet.Substring(0, 150) + "..."
                            : result.ContentSnippet;
                        sb.AppendLine($"   **内容:** {snippet}");
                    }
                    sb.AppendLine();
                }
                
                if (TotalResults > DisplayedResults)
                {
                    sb.AppendLine($"... 他 {TotalResults - DisplayedResults} 件の結果があります");
                }
                
                return sb.ToString();
            }
        }
    }
}
