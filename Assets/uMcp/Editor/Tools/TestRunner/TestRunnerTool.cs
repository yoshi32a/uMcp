using Microsoft.Extensions.DependencyInjection;
using uMCP.Editor.Core;
using UnityEngine;

namespace uMCP.Editor.Tools
{
    /// <summary>テスト実行用MCPツール</summary>
    [CreateAssetMenu(fileName = "TestRunnerTool", menuName = "uMCP/Tools/Test Runner Tool")]
    public class TestRunnerTool : UMcpToolBuilder
    {
        void OnEnable()
        {
            toolName = "Test Runner Tool";
            description = "Run Unity tests in EditMode and PlayMode, get test results";
        }

        public override void Build(IServiceCollection services)
        {
            services.AddSingleton<TestRunnerToolImplementation>();
        }
    }
}
