using System;
using System.Collections.Generic;

namespace uMCP.Editor.Tools
{
    /// <summary>軽量ドキュメントインデックス（段階的読み込み用）</summary>
    [Serializable]
    public class LightweightDocumentationIndex
    {
        /// <summary>インデックスバージョン</summary>
        public string Version { get; set; } = "1.1";
        
        /// <summary>インデックス作成時刻</summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>Unityバージョン</summary>
        public string UnityVersion { get; set; }
        
        /// <summary>ドキュメントパス</summary>
        public string DocumentationPath { get; set; }
        
        /// <summary>軽量エントリ数</summary>
        public int TotalEntries { get; set; }
        
        /// <summary>キーワード→エントリインデックスのマップ（常時メモリ）</summary>
        public Dictionary<string, List<int>> KeywordIndex { get; set; } = new Dictionary<string, List<int>>();
        
        /// <summary>詳細インデックスファイルパス</summary>
        public string DetailIndexPath { get; set; }
    }
    
    /// <summary>軽量インデックスエントリ（オンデマンド読み込み用）</summary>
    [Serializable]
    public class LightweightIndexEntry
    {
        /// <summary>ファイルパス</summary>
        public string FilePath { get; set; }
        
        /// <summary>タイトル</summary>
        public string Title { get; set; }
        
        /// <summary>ドキュメントタイプ</summary>
        public string DocumentType { get; set; }
        
        /// <summary>相対URL</summary>
        public string RelativeUrl { get; set; }
        
        /// <summary>上位キーワード（5個まで）</summary>
        public List<string> TopKeywords { get; set; } = new List<string>();
        
        /// <summary>ファイル内位置（バイトオフセット）</summary>
        public long FileOffset { get; set; }
        
        /// <summary>データ長</summary>
        public int DataLength { get; set; }
    }
    
    /// <summary>詳細エントリ（検索時のみ使用）</summary>
    [Serializable]
    public class DetailedIndexEntry : LightweightIndexEntry
    {
        /// <summary>全キーワード</summary>
        public List<string> AllKeywords { get; set; } = new List<string>();
        
        /// <summary>コンテンツスニペット</summary>
        public string ContentSnippet { get; set; }
        
        /// <summary>ファイルサイズ</summary>
        public long FileSize { get; set; }
        
        /// <summary>最終更新時刻</summary>
        public DateTime LastModified { get; set; }
    }
}