using System;

namespace uMCP.Editor.Core.Attributes
{
    /// <summary>MCPツールクラスを示す属性</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class McpServerToolTypeAttribute : Attribute
    {
    }

    /// <summary>MCPツールメソッドを示す属性</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class McpServerToolAttribute : Attribute
    {
    }

    /// <summary>説明を提供する属性</summary>
    [AttributeUsage(AttributeTargets.All)]
    public class DescriptionAttribute : Attribute
    {
        public string Description { get; }

        public DescriptionAttribute(string description)
        {
            Description = description;
        }
    }
}