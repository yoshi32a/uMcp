using System;

namespace uMCP.Editor.Tools
{
    /// <summary>ドキュメント検索結果のアイテム</summary>
    [Serializable]
    public class DocumentationSearchResult
    {
        /// <summary>ファイルパス</summary>
        public string FilePath { get; set; }
        
        /// <summary>ドキュメントタイトル</summary>
        public string Title { get; set; }
        
        /// <summary>ドキュメントタイプ（Manual/ScriptReference）</summary>
        public string DocumentType { get; set; }
        
        /// <summary>マッチした内容の抜粋</summary>
        public string ContentSnippet { get; set; }
        
        /// <summary>検索スコア（0.0-1.0）</summary>
        public float Score { get; set; }
        
        /// <summary>ドキュメントURL（相対パス）</summary>
        public string RelativeUrl { get; set; }
    }
}
