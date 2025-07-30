using System;
using System.IO;
using uMCP.Editor.Core.Protocol;

namespace uMCP.Editor.Core
{
    /// <summary>MCPセッション情報</summary>
    public class McpSession
    {
        public string SessionId { get; set; }
        public SimpleMcpServer McpServer { get; set; }
        public MemoryStream InputStream { get; set; }
        public MemoryStream OutputStream { get; set; }
        public DateTime LastAccessed { get; set; } = DateTime.Now;
    }
}