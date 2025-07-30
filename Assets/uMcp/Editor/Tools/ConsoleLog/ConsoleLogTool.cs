using uMCP.Editor.Core;
using uMCP.Editor.Core.DependencyInjection;
using UnityEngine;

namespace uMCP.Editor.Tools
{
    /// <summary>コンソールログ管理用MCPツール</summary>
    [CreateAssetMenu(fileName = "ConsoleLogTool", menuName = "uMCP/Tools/Console Log Tool")]
    public class ConsoleLogTool : UMcpToolBuilder
    {
        void OnEnable()
        {
            toolName = "Console Log Tool";
            description = "Manage Unity console logs, get current logs, and clear console";
        }

        public override void Build(ServiceCollectionBuilder builder)
        {
            builder.AddSingleton(new ConsoleLogToolImplementation());
        }
    }
}
