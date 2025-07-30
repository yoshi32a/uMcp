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

        /// <summary>セッションを削除します</summary>
        public void RemoveSession(string sessionId)
        {
            lock (lockObject)
            {
                if (sessions.TryGetValue(sessionId, out var session))
                {
                    session.InputStream?.Dispose();
                    session.OutputStream?.Dispose();
                    sessions.Remove(sessionId);
                }
            }
        }

        /// <summary>古いセッションをクリーンアップします</summary>
        public void CleanupOldSessions(TimeSpan maxAge)
        {
            lock (lockObject)
            {
                var cutoffTime = DateTime.Now - maxAge;
                var sessionsToRemove = new List<string>();

                foreach (var kvp in sessions)
                {
                    if (kvp.Value.LastAccessed < cutoffTime)
                    {
                        sessionsToRemove.Add(kvp.Key);
                    }
                }

                foreach (var sessionId in sessionsToRemove)
                {
                    RemoveSession(sessionId);
                }
            }
        }

        /// <summary>全セッションをクリアします</summary>
        public void ClearAllSessions()
        {
            lock (lockObject)
            {
                foreach (var session in sessions.Values)
                {
                    session.InputStream?.Dispose();
                    session.OutputStream?.Dispose();
                }

                sessions.Clear();
            }
        }
    }
}
