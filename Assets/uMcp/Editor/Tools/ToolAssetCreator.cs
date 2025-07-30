using UnityEditor;
using UnityEngine;

namespace uMCP.Editor.Tools
{
    /// <summary>ツールアセットを自動作成するクラス</summary>
    public static class ToolAssetCreator
    {
        [MenuItem("uMCP/Create Default Tool Assets")]
        public static void CreateDefaultToolAssets()
        {
            // Unity Info Tool を作成
            var unityInfoTool = ScriptableObject.CreateInstance<UnityInfoTool>();
            AssetDatabase.CreateAsset(unityInfoTool, "Assets/uMcp/UnityInfoTool.asset");

            // Asset Management Tool を作成
            var assetTool = ScriptableObject.CreateInstance<AssetManagementTool>();
            AssetDatabase.CreateAsset(assetTool, "Assets/uMcp/AssetManagementTool.asset");

            // Console Log Tool を作成
            var consoleTool = ScriptableObject.CreateInstance<ConsoleLogTool>();
            AssetDatabase.CreateAsset(consoleTool, "Assets/uMcp/ConsoleLogTool.asset");

            // Test Runner Tool を作成
            var testTool = ScriptableObject.CreateInstance<TestRunnerTool>();
            AssetDatabase.CreateAsset(testTool, "Assets/uMcp/TestRunnerTool.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[uMCP] All default tool assets created successfully!");
        }

        [MenuItem("uMCP/Remove Default Tool Assets")]
        public static void RemoveDefaultToolAssets()
        {
            AssetDatabase.DeleteAsset("Assets/uMcp/UnityInfoTool.asset");
            AssetDatabase.DeleteAsset("Assets/uMcp/AssetManagementTool.asset");
            AssetDatabase.DeleteAsset("Assets/uMcp/ConsoleLogTool.asset");
            AssetDatabase.DeleteAsset("Assets/uMcp/TestRunnerTool.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[uMCP] All tool assets removed.");
        }
    }
}