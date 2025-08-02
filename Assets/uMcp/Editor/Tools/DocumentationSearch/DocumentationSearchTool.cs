using UnityEngine;
using uMCP.Editor.Core;
using uMCP.Editor.Core.DependencyInjection;

namespace uMCP.Editor.Tools
{
    /// <summary>Unity ドキュメント検索ツール</summary>
    [CreateAssetMenu(menuName = "uMCP/Tools/Documentation Search Tool", fileName = "DocumentationSearchTool")]
    public class DocumentationSearchTool : UMcpToolBuilder
    {
        [Header("Documentation Search Settings")] [SerializeField]
        string defaultSearchType = "All";

        [SerializeField] int defaultMaxResults = 10;

        /// <summary>デフォルト検索タイプ</summary>
        public string DefaultSearchType => defaultSearchType;

        /// <summary>デフォルト最大結果数</summary>
        public int DefaultMaxResults => defaultMaxResults;

        public override void Build(ServiceCollectionBuilder builder)
        {
            builder.AddSingleton(new DocumentationSearchToolImplementation());
        }

        void Reset()
        {
            toolName = "Documentation Search";
            description = "Unity公式ドキュメントを検索してAIアシスタントが参照できるようにします";
            defaultSearchType = "All";
            defaultMaxResults = 10;
        }
    }
}
