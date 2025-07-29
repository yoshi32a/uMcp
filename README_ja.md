# Unity MCP Server

Unity EditorとAIアシスタントがリアルタイムで連携できる、強力なModel Context Protocol (MCP) サーバー実装です。

## 特徴

### 🚀 MCPサーバーコア
- **HTTPサーバー**: デフォルトで `localhost:49001/umcp/` で動作
- **JSON-RPC 2.0**: 完全なMCPプロトコル準拠
- **リアルタイム通信**: Unity Editorとの直接統合
- **自動起動**: Unity Editor起動時に自動開始

### 🛠️ ビルトインツール群

#### Unity情報ツール
- `get_unity_info` - Unity エディターとプロジェクトの詳細情報取得
- `get_scene_info` - 現在のシーン構造分析
- `log_message` - Unity コンソールへのメッセージ出力

#### アセット管理ツール
- `refresh_assets` - Unity アセットデータベースのリフレッシュ
- `save_project` - 現在のプロジェクトとアセットの保存
- `find_assets` - フィルターとフォルダーによるアセット検索
- `get_asset_info` - アセットの詳細情報取得
- `reimport_asset` - 特定アセットの強制再インポート

#### コンソールログツール
- `get_console_logs` - フィルタリング機能付きUnity コンソールログ取得
- `clear_console_logs` - 全コンソールログのクリア
- `log_to_console` - カスタムメッセージのコンソール出力
- `get_log_statistics` - コンソールログ統計情報の取得

#### テストランナーツール
- `run_edit_mode_tests` - タイムアウト制御付きEditModeテスト実行
- `run_play_mode_tests` - ドメインリロード制御付きPlayModeテスト実行
- `get_available_tests` - モード別利用可能テスト一覧取得（EditMode/PlayMode/All）

## インストール方法

### 方法1: Package Manager（Git URL）
1. Unity Package Managerを開く
2. 「+」→「Add package from git URL」をクリック
3. URL入力：`https://github.com/yoshi32a/uMcp.git?path=Assets/uMcp`

### 方法2: 手動インストール
1. 最新リリースをダウンロード
2. `Assets/uMcp/`フォルダーをプロジェクトの`Assets/`またはパッケージディレクトリにコピー
3. Unityが自動的にパッケージを検出・インポート

### 方法3: UnityPackageファイル
1. Releasesページから`.unitypackage`ファイルをダウンロード
2. Unity Editorで「Assets → Import Package → Custom Package」を選択
3. ダウンロードした`.unitypackage`ファイルを選択してインポート

## クイックスタート

1. **自動サーバー起動**: Unity起動時にサーバーが自動開始
2. **手動制御**: `Tools > uMCP`メニューでサーバー管理
3. **ツールアセット作成**: `Tools > uMCP > Create Default Tool Assets`を実行
4. **接続テスト**: MCP Inspectorを`http://localhost:49001/umcp/`に接続

## 設定

`Tools > uMCP > Open Settings`から設定にアクセス：

- **サーバーアドレス**: デフォルト `127.0.0.1:49001`
- **サーバーパス**: デフォルト `/umcp/`
- **自動起動**: 自動サーバー起動の有効/無効
- **デバッグモード**: 詳細なリクエスト/レスポンスログの有効化
- **CORS対応**: Web基盤MCPクライアント対応

## MCPクライアント統合

### MCP Inspector
1. [MCP Inspector](https://inspector.mcp.run/)を開く
2. Transportを`HTTP`に設定
3. URL入力：`http://localhost:49001/umcp/`
4. Connectをクリック

### Claude Code
MCPクライアント設定に以下を追加：
```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "curl",
      "args": ["-X", "POST", "http://localhost:49001/umcp/"]
    }
  }
}
```

## 開発

### カスタムツールの作成

1. `UMcpToolBuilder`を継承する新しいScriptableObjectを作成
2. `Build`メソッドを実装してツールサービスを登録
3. `[McpServerToolType]`と`[McpServerTool]`属性を使用

サンプルコード：
```csharp
[CreateAssetMenu(fileName = "MyCustomTool", menuName = "uMCP/Tools/My Custom Tool")]
public class MyCustomTool : UMcpToolBuilder
{
    public override void Build(IServiceCollection services)
    {
        services.AddSingleton<MyCustomToolImplementation>();
    }
}

[McpServerToolType, Description("私のカスタムツール")]
internal sealed class MyCustomToolImplementation
{
    [McpServerTool, Description("カスタムアクションを実行")]
    public async ValueTask<object> DoSomething()
    {
        await UniTask.SwitchToMainThread();
        return new { success = true, message = "カスタムアクション完了" };
    }
}
```

## 高度な機能

### ドメインリロード制御付きPlayModeテスト実行

Unity MCP Serverは、ドメインリロード最適化による高度なPlayModeテスト実行機能を搭載：

**主要機能：**
- **ドメインリロード制御**: PlayModeテスト実行時に自動的にドメインリロードを無効化し、高速実行を実現
- **高性能実行**: PlayModeテストが数分から約0.25秒に短縮
- **設定自動復元**: テスト完了後にEditorSettingsを安全に復元
- **HTTP互換**: 高速実行によりMCP HTTPリクエスト経由でもPlayModeテストが実用的

**パラメータ：**
- `disableDomainReload`（デフォルト：`true`） - PlayModeテスト実行時のドメインリロード動作制御
- `timeoutSeconds` - テスト実行のタイムアウト設定
- アセンブリとカテゴリーフィルタリング対応

**パフォーマンス比較：**
- 従来のPlayModeテスト：ドメインリロードありで60秒以上
- 最適化されたPlayModeテスト：ドメインリロードなしで約0.25秒

### EditModeテスト実行

- 標準Unity Test Framework統合
- 設定可能なタイムアウトとフィルタリングオプション
- 一般的なテストスイートでの実行時間：約0.6秒

## 動作要件

- **Unity**: 6000.0以降
- **UniTask**: 2.3.3以降（自動インストール）
- **.NET**: Unity対応の.NET実装

## トラブルシューティング

### よくある問題

**サーバーが起動しない：**
- ポート49001が利用可能か確認
- 設定でポート番号を変更
- Unity コンソールでエラーメッセージを確認

**ツールが表示されない：**
- ツールアセットが作成されているか確認（`Tools > uMCP > Create Default Tool Assets`）
- サーバーを再起動（`Tools > uMCP > Restart Server`）
- アセンブリコンパイルエラーを確認

**MCP Inspector接続失敗：**
- サーバーが動作しているか確認（`Tools > uMCP > Show Server Info`）
- ファイアウォール設定を確認
- Webクライアント使用時はCORSが有効か確認

**PlayModeテストのタイムアウトや失敗：**
- `disableDomainReload`が`true`に設定されているか確認（デフォルト）
- UnityがPlay Mode中でないか確認（Play Mode中はテスト実行不可）
- Unity Test Frameworkが適切に設定されているか確認
- 手動テスト用にUnity Test Runnerウィンドウをフォールバックとして使用

**EditModeテストが実行されない：**
- Unity コンソールでコンパイルエラーを確認
- テストアセンブリが適切に設定されているか確認
- テストメソッドがUnity Test Frameworkの規約に従っているか確認

## アーキテクチャ

```
Unity MCP Server
├── Core/
│   ├── UMcpServer.cs          # HTTPサーバー実装
│   ├── UMcpServerManager.cs   # Unity統合・ライフサイクル
│   └── UMcpToolBuilder.cs     # ツール登録基底クラス
├── Settings/
│   └── UMcpSettings.cs        # 設定管理
└── Tools/
    ├── UnityInfoTool.cs       # Unity情報ツール
    ├── AssetManagementTool.cs # アセット操作
    ├── ConsoleLogTool.cs      # コンソールログ管理
    └── TestRunnerTool.cs      # テスト実行ツール
```

## 貢献

1. リポジトリをフォーク
2. フィーチャーブランチを作成
3. 変更を実装
4. 必要に応じてテストを追加
5. プルリクエストを提出

## ライセンス

このプロジェクトはMITライセンスの下で公開されています - 詳細は[LICENSE.md](LICENSE.md)ファイルを参照してください。

## 謝辞

- [Unity Natural MCP](https://github.com/notargs/UnityNaturalMCP)にインスパイア
- [UniTask](https://github.com/Cysharp/UniTask)で構築
- [Model Context Protocol](https://github.com/modelcontextprotocol)を実装