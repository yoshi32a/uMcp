using NUnit.Framework;
using UnityEngine;
using UnityEditor;

public class EditorTests
{
    [Test]
    public void BasicEditModeTest()
    {
        // 基本的なEditModeテスト
        int result = 10 + 5;
        Assert.AreEqual(15, result, "10 + 5 should equal 15");
    }
    
    [Test]
    public void EditorApplicationTest()
    {
        // UnityEditor APIのテスト
        Assert.IsTrue(EditorApplication.isPlayingOrWillChangePlaymode == false, "Should not be in play mode");
        Assert.IsNotNull(EditorApplication.applicationPath, "Editor application path should exist");
    }
    
    [Test]
    public void AssetDatabaseTest()
    {
        // AssetDatabase APIのテスト
        string[] allAssets = AssetDatabase.GetAllAssetPaths();
        Assert.IsNotNull(allAssets, "Asset paths should not be null");
        Assert.Greater(allAssets.Length, 0, "Should have at least some assets");
    }
    
    [Test]
    [Category("MCP")]
    public void McpEditModeTest()
    {
        // MCP関連のEditModeテスト
        Assert.IsTrue(Application.isEditor, "Should be running in editor");
        Assert.IsNotNull(Application.dataPath, "Data path should exist");
    }
    
    [Test]
    public void StringManipulationTest()
    {
        // 文字列操作テスト
        string input = "Hello World";
        string result = input.ToUpper();
        Assert.AreEqual("HELLO WORLD", result, "String should be converted to uppercase");
    }
}
