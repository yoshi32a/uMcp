using System;

namespace uMCP.Editor.Core
{
    /// <summary>MCPセッション情報</summary>
    public class McpSession
    {
        public string SessionId { get; set; }
        public DateTime LastAccessed { get; set; } = DateTime.Now;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}