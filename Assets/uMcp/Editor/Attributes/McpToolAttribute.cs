using System;

namespace uMCP.Editor.Attributes
{
    /// <summary>MCPツールクラスを識別するための属性</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class McpToolAttribute : Attribute
    {
        /// <summary>ツールの説明</summary>
        public string Description { get; }

        /// <summary>ツールの順序（低い値が先に表示される）</summary>
        public int Order { get; set; } = 0;

        /// <summary>ツールが有効かどうか</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>McpToolAttributeの新しいインスタンスを初期化します</summary>
        /// <param name="description">ツールの説明</param>
        public McpToolAttribute(string description = null)
        {
            Description = description;
        }
    }
}