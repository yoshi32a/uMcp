using Microsoft.Extensions.DependencyInjection;
using uMCP.Editor.Core;
using UnityEngine;

namespace uMCP.Editor.Tools
{
    /// <summary>Unity情報を取得するMCPツール</summary>
    [CreateAssetMenu(fileName = "UnityInfoTool", menuName = "uMCP/Tools/Unity Info Tool")]
    public class UnityInfoTool : UMcpToolBuilder
    {
        void OnEnable()
        {
            toolName = "Unity Info Tool";
            description = "Get Unity editor and project information";
        }

        public override void Build(IServiceCollection services)
        {
            // 現在のMCPライブラリではサービス登録のみ行う
            services.AddSingleton<UnityInfoToolImplementation>();
        }
    }
}
