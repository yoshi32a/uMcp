# Unity MCP Server

Unity EditorとAIアシスタントがリアルタイムでインテリジェントに連携できる、強力なModel Context Protocol (MCP) サーバー実装です。

## 特徴

### 🚀 MCPサーバーコア
- **HTTPサーバー**: デフォルトで `localhost:49001/umcp/` で動作
- **JSON-RPC 2.0**: 完全なMCPプロトコル準拠
- **リアルタイム通信**: Unity Editorとの直接統合
- **自動起動**: Unity Editor起動時に自動開始

### 🛠️ ビルトインツール群（全21ツール）

#### 🎯 Unity情報ツール（5ツール）
- `get_unity_info` - Unity エディターとプロジェクトの詳細情報取得
- `get_scene_info` - 現在のシーン構造分析
- `get_hierarchy_analysis` - 指定GameObjectとその子階層の構造を詳細分析
- `get_game_object_info` - 指定GameObjectの詳細情報を取得
- `get_prefab_info` - 指定Prefabの詳細情報を取得

#### 📁 アセット管理ツール（5ツール）
- `refresh_assets` - Unity アセットデータベースのリフレッシュ
- `save_project` - 現在のプロジェクトとアセットの保存
- `find_assets` - フィルターとフォルダーによるアセット検索
- `get_asset_info` - アセットの詳細情報取得
- `reimport_asset` - 特定アセットの強制再インポート

#### 🐛 コンソールログツール（4ツール）
- `get_console_logs` - フィルタリング機能付きUnity コンソールログ取得（errorsOnlyバグ修正済み）
- `clear_console_logs` - 全コンソールログのクリア
- `log_to_console` - カスタムメッセージのコンソール出力
- `get_log_statistics` - コンソールログ統計情報の取得

#### 🧪 テストランナーツール（3ツール）
- `run_edit_mode_tests` - タイムアウト制御付きEditModeテスト実行
- `run_play_mode_tests` - ドメインリロード制御付きPlayModeテスト実行
- `get_available_tests` - モード別利用可能テスト一覧取得（EditMode/PlayMode/All）

#### ⚙️ エディタ拡張ツール（1ツール）
- `execute_editor_method` - コンパイル済みエディタ拡張の静的メソッドを実行

#### 🧠 ワークフロー提案ツール（2ツール）**NEW!**
- `get_next_action_suggestions` - 現在の状態から推奨される次のMCPツール実行を提案
- `get_workflow_patterns` - Markdownファイルから読み込んだワークフローパターンを取得

### 🆕 最新バージョンの新機能
- **🧠 ワークフロー提案**: コンテキスト対応の次アクション提案
- **📝 Markdownワークフロー**: 編集しやすいワークフロー定義
- **🎯 スマートトリガー**: コンテキストに基づく自動ツール連携
- **🐛 バグ修正**: `get_console_logs` errorsOnlyフィルタリング問題を解決

## 事前準備

### システム要件
- **Unity**: 2022.3 LTS以降
- **UniTask**: 2.3.3以降（必須依存関係）

### UniTaskのインストール
1. Unity Package Managerを開く
2. 「+」→「Add package from git URL」をクリック
3. URL入力：`https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`

## インストール方法

### Package Manager（推奨）
1. まず[UniTaskをインストール](#unitaskのインストール)してください
2. Unity Package Managerを開く
3. 「+」→「Add package from git URL」をクリック
4. URL入力：`https://github.com/yoshi32a/uMcp.git?path=Assets/uMcp`

### 手動インストール
1. まず[UniTaskをインストール](#unitaskのインストール)してください
2. [GitHub Releases](https://github.com/yoshi32a/uMcp/releases)から最新リリースをダウンロード
3. `Assets/uMcp/`フォルダーをプロジェクトの`Assets/`またはパッケージディレクトリにコピー
4. Unityが自動的にパッケージを検出・インポート

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

### Claude CLI
Unity MCP ServerをClaude CLIで使用するには、以下のコマンドで設定に追加します：

```bash
claude mcp add -s project --transport http unity-mcp-server http://localhost:49001/umcp/
```

これによりUnity MCP Serverがプロジェクトの MCP 設定に追加されます。

**注意：** Claude CLIを使用する前に、Unity EditorでMCPサーバーが起動していることを確認してください。

### GitHub Copilot
GitHub CopilotのMCP統合で使用する場合：

1. IDEにGitHub Copilot拡張機能をインストール
2. Copilotの設定/構成を開く
3. Unity MCP Serverエンドポイントを追加：
   - サーバーURL: `http://localhost:49001/umcp/`
   - プロトコル: HTTP
   - メソッド: POST

注意：GitHub CopilotのMCPサポートは特定バージョンまたはプレビュー機能が必要な場合があります。

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

**パフォーマンス向上：**
- ドメインリロードを無効化することで、PlayModeテストの実行時間を大幅に短縮
- HTTP経由でのMCPリクエストでも実用的な速度で実行可能

### EditModeテスト実行

- 標準Unity Test Framework統合
- 設定可能なタイムアウトとフィルタリングオプション

## 動作要件

- **Unity**: 2022.3 LTS以降
- **UniTask**: 2.3.3以降
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

## Markdownワークフローシステム

**コンテキスト対応のインテリジェントワークフロー提案**

新機能として、Markdownファイルでワークフローを定義し、現在の作業コンテキストに基づいて次のアクションを自動提案するシステムを搭載：

### 主要機能
- **Markdownベース定義**: 開発者が簡単に編集可能な`.md`ファイルでワークフロー定義
- **動的提案システム**: 実行したツールと作業コンテキストに基づく次アクション提案  
- **4つの組み込みワークフロー**: エディタ拡張開発、エラー調査、テスト実行、アセット管理
- **トリガーシステム**: ツール実行後の自動推奨とキーワードベースマッチング
- **パラメータ付き実行**: 各ステップに最適なパラメータを自動設定

### ワークフローファイル例
```markdown
# エディタ拡張開発ワークフロー

## ステップ

### 1. アセットリフレッシュ
- tool: refresh_assets
- 説明: 新しいスクリプトファイルを認識させる

### 2. メソッド実行
- tool: execute_editor_method
- 説明: 作成したメソッドを実行
```

## アーキテクチャ

```
Assets/uMcp/
├── package.json               # Unityパッケージ定義
├── README.md                  # 英語ドキュメント
├── Workflows/                 # Markdownワークフロー定義（NEW!）
│   ├── editor-extension-workflow.md
│   ├── error-investigation-workflow.md
│   └── workflow-triggers.md
└── Editor/                    # エディター拡張実装（21ツール）
    ├── uMCP.Editor.asmdef     # アセンブリ定義
    ├── Attributes/            # カスタム属性
    │   ├── McpToolAttribute.cs        # ツールクラス属性
    │   └── McpToolMethodAttribute.cs  # ツールメソッド属性
    ├── Core/                  # MCPサーバーコア
    │   ├── UMcpServer.cs              # HTTPサーバー実装
    │   ├── UMcpServerManager.cs       # Unity統合・ライフサイクル管理
    │   └── UMcpToolBuilder.cs         # ツール登録基底クラス
    ├── Settings/              # 設定管理
    │   └── UMcpSettings.cs            # プロジェクト設定ScriptableSingleton
    └── Tools/                 # ビルトインツール実装
        ├── UnityInfo/         # Unity情報ツール（5ツール）
        ├── AssetManagement/   # アセット管理ツール（5ツール）
        ├── ConsoleLog/        # コンソールログツール（4ツール）
        ├── TestRunner/        # テスト実行ツール（3ツール）
        ├── EditorExtension/   # エディタ拡張ツール（1ツール）
        └── ToolWorkflow/      # ワークフロー提案ツール（2ツール・NEW!）
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