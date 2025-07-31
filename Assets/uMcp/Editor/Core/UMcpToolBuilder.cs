using uMCP.Editor.Core.DependencyInjection;
using UnityEngine;

namespace uMCP.Editor.Core
{
    /// <summary>ツールビルダーの基底クラス</summary>
    public abstract class UMcpToolBuilder : ScriptableObject
    {
        /// <summary>ツールの名前</summary>
        [Header("Tool Information")] [SerializeField]
        protected string toolName;

        /// <summary>ツールの説明</summary>
        [SerializeField] protected string description;

        /// <summary>ツールの有効/無効状態</summary>
        [SerializeField] protected bool isEnabled = true;

        /// <summary>ツール名を取得</summary>
        public virtual string ToolName => toolName;

        /// <summary>ツールの説明を取得</summary>
        public virtual string Description => description;

        /// <summary>ツールが有効かどうかを取得</summary>
        public bool IsEnabled => isEnabled;

        /// <summary>サービスコレクションにツールを構築する抽象メソッド</summary>
        public abstract void Build(ServiceCollectionBuilder builder);
    }
}
