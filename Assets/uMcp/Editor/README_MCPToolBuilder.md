# MCP Tool Builder 使用方法

## 概要
ScriptableObjectベースでMCPツールを登録するためのサンプル実装です。

## 作成したファイル

### 1. SceneAnalysisToolBuilder.cs
特定のツール（SceneAnalysisTool）を登録するシンプルなBuilder

### 2. CustomMCPToolBuilder.cs
複数のツールクラスを動的に登録できる汎用Builder

## 使用手順

1. **ScriptableObjectの作成**
   - Unity Editorで右クリック
   - Create > UnityNaturalMCP > Scene Analysis Tool Builder
   - または Create > UnityNaturalMCP > Custom MCP Tool Builder

2. **設定**
   - CustomMCPToolBuilderの場合、InspectorでtoolClassNamesを編集
   - 登録したいツールのフルクラス名を指定

3. **サーバー更新**
   - Edit > Project Settings > Unity Natural MCP > Refresh
   - MCPサーバーが再起動し、新しいツールが登録される

## 登録方法の比較

### 属性ベース (既存)
```csharp
[McpServerToolType]
public class MyTool
{
    [McpServerTool]
    public string MyMethod() { ... }
}
```

### ScriptableObjectベース (新規)
```csharp
[CreateAssetMenu(...)]
public class MyBuilder : McpBuilderScriptableObject
{
    public override void Build(IMcpServerBuilder builder)
    {
        builder.WithTools<MyTool>();
    }
}
```

## 利点
- Unity Editorでの視覚的な管理
- 実行時の動的な設定変更
- プロジェクト固有のツール構成