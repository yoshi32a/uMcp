# Unity MCP Server

AIアシスタントがUnityプロジェクトとリアルタイムでやり取りできる強力なModel Context Protocol (MCP) サーバー実装です。

## 機能

### 🚀 コアMCPサーバー
- **HTTP + JSON-RPC 2.0** プロトコル `127.0.0.1:49001/umcp/` で動作
- **自動起動** Unity Editor と連携
- **リアルタイム** Unity プロジェクト操作
- **カスタムツール** ScriptableObject統合フレームワーク

### 🛠️ ビルトインツール (21個)

| カテゴリ | ツール | 主要機能 |
|----------|-------|----------|
| **Unity情報** | `get_unity_info`, `get_scene_info`, `get_hierarchy_analysis`, `get_game_object_info`, `get_prefab_info` | エディタ詳細、シーン分析、オブジェクト詳細 |
| **アセット管理** | `refresh_assets`, `save_project`, `find_assets`, `get_asset_info` | 完全なアセットライフサイクル管理 |
| **コンソールログ** | `get_console_logs`, `clear_console_logs`, `log_to_console`, `get_log_statistics` | ログ管理と分析 |
| **テスト実行** | `run_edit_mode_tests`, `run_play_mode_tests`, `get_available_tests` | 最適化されたテスト実行 |
| **エディタ拡張** | `execute_editor_method` | カスタムメソッド実行 |
| **ワークフロー** | `get_next_action_suggestions`, `get_workflow_patterns` | インテリジェントな提案システム |

## インストール

### 前提条件
Unity Package Manager (`+` → `Add package from git URL`) で以下の依存関係をインストール:

1. **UniTask**: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
2. **System.Text.Json**: Unity 2022.3以降では組み込み済み

### Unity MCP Server のインストール

**Package Manager 経由 (推奨):**
```
https://github.com/yoshi32a/uMcp.git?path=Assets/uMcp
```

**その他の方法:**
- **手動**: [Releases](https://github.com/yoshi32a/uMcp/releases) からダウンロード → `Assets/` または `Packages/` に展開
- **UnityPackage**: [Releases](https://github.com/yoshi32a/uMcp/releases) から `.unitypackage` をダウンロード → Unity でインポート

## クイックスタート

1. **サーバー自動起動**: Unity ロード時に自動でサーバーが起動
2. **手動制御**: `Tools > uMCP` メニューでサーバー管理
3. **ツールアセット作成**: `Tools > uMCP > Create Default Tool Assets` を実行
4. **接続テスト**: MCPクライアントを `http://127.0.0.1:49001/umcp/` に接続

## 使用方法

### 設定
`Tools > uMCP > Open Settings` で設定にアクセス:
- **サーバー**: `127.0.0.1:49001/umcp/` (デフォルト)
- **自動起動**: 自動サーバー起動
- **デバッグモード**: リクエスト/レスポンスのログ出力
- **CORS**: Webクライアント対応

### MCPクライアント接続

| クライアント | 接続方法 |
|-------------|----------|
| **MCP Inspector** | [inspector.mcp.run](https://inspector.mcp.run/) → HTTP → `http://127.0.0.1:49001/umcp/` |
| **Claude CLI** | `claude mcp add -s project --transport http unity-mcp-server http://127.0.0.1:49001/umcp/` |
| **カスタムクライアント** | `http://127.0.0.1:49001/umcp/` へ JSON-RPC 2.0 で HTTP POST |

## 開発

### カスタムツール
属性駆動登録でScriptableObjectベースツールを作成:

```csharp
[McpServerToolType, Description("私のカスタムツール")]
internal sealed class MyCustomToolImplementation
{
    [McpServerTool, Description("カスタム処理を実行")]
    public async ValueTask<StandardResponse> DoSomething(
        [Description("入力パラメータ")] string input = "default")
    {
        await UniTask.SwitchToMainThread(); // Unity API アクセスに必須
        return new StandardResponse 
        { 
            Success = true, 
            FormattedOutput = $"処理完了: {input}" 
        };
    }
}
```

**重要なポイント:**
- Unity API呼び出し前に `await UniTask.SwitchToMainThread()` を使用
- MCPクライアント統合のため `[Description]` を追加
- 統一された `StandardResponse` クラスを使用
- シリアライゼーション可能なオブジェクトを返す

### レスポンス統一化
全ツールで統一された `StandardResponse` クラスを使用:

```csharp
public class StandardResponse
{
    public bool Success { get; set; }
    [JsonPropertyName("formatted_output")]
    public string FormattedOutput { get; set; }
    public string Error { get; set; }
    public string Message { get; set; }
}
```

## システム要件

- **Unity 2022.3 LTS** 以降
- **UniTask 2.3.3+** (非同期処理)
- **System.Text.Json 9.0.7+** (JSON シリアライゼーション)

## ライセンス

MIT License - 詳細は [LICENSE](LICENSE) をご覧ください。

## サポート

- **問題報告**: [GitHub Issues](https://github.com/yoshi32a/uMcp/issues)
- **参考**: [Unity Natural MCP](https://github.com/johniwasz/unity-natural-mcp)