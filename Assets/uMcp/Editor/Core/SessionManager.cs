using System;
using System.Collections.Generic;

namespace uMCP.Editor.Core
{
    /// <summary>MCPセッション管理クラス</summary>
    public class SessionManager
    {
        readonly Dictionary<string, McpSession> sessions = new();
        readonly object lockObject = new();

        /// <summary>新しいセッションIDを生成します</summary>
        public string CreateSession()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>セッションを取得または作成します</summary>
        public McpSession GetOrCreateSession(string sessionId)
        {
            lock (lockObject)
            {
                if (sessions.TryGetValue(sessionId, out var session))
                {
                    session.LastAccessed = DateTime.Now;
                    return session;
                }

                var newSession = new McpSession
                {
                    SessionId = sessionId,
                    LastAccessed = DateTime.Now
                };

                sessions[sessionId] = newSession;
                return newSession;
            }
        }
    }
}
