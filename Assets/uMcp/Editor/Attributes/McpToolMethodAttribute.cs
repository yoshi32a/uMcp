using System;

namespace uMCP.Editor.Attributes
{
    /// <summary>MCPツールメソッドを識別するための属性</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class McpToolMethodAttribute : Attribute
    {
        /// <summary>ツールメソッドの名前</summary>
        public string Name { get; }

        /// <summary>ツールメソッドの説明</summary>
        public string Description { get; }

        /// <summary>McpToolMethodAttributeの新しいインスタンスを初期化します</summary>
        /// <param name="name">ツールメソッドの名前</param>
        /// <param name="description">ツールメソッドの説明</param>
        public McpToolMethodAttribute(string name, string description = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
        }
    }
}