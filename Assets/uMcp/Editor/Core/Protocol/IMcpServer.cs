using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace uMCP.Editor.Core.Protocol
{
    /// <summary>MCPサーバーのインターフェース</summary>
    public interface IMcpServer
    {
        /// <summary>サーバーを実行</summary>
        UniTask RunAsync(CancellationToken cancellationToken);
    }
}