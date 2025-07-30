using System;

namespace uMCP.Editor.Core.Attributes
{
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