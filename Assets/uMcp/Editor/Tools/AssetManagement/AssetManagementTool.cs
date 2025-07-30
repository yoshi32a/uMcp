using uMCP.Editor.Core;
using uMCP.Editor.Core.DependencyInjection;
using UnityEngine;

namespace uMCP.Editor.Tools
{
    /// <summary>アセット管理用MCPツール</summary>
    [CreateAssetMenu(fileName = "AssetManagementTool", menuName = "uMCP/Tools/Asset Management Tool")]
    public class AssetManagementTool : UMcpToolBuilder
    {
        void OnEnable()
        {
            toolName = "Asset Management Tool";
            description = "Manage Unity assets, refresh database, and project operations";
        }

        public override void Build(ServiceCollectionBuilder builder)
        {
            builder.AddSingleton(new AssetManagementToolImplementation());
        }
    }
}
