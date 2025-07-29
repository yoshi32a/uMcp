# CLAUDE.md

このファイルは、このリポジトリでコードを扱う際のClaude Code (claude.ai/code) への指針を提供します。

## 言語設定

**重要: このプロジェクトでは必ず日本語で回答してください。**

## プロジェクト概要

**Unity MCP Server** は、完全に実装されたModel Context Protocol (MCP) サーバーパッケージです。Unity Natural MCPを参考にしつつ、独自の改良を加えたUnity Editor統合MCPサーバーとして開発されました。

**Unityバージョン:** 6000.0以上  
**パッケージ名:** com.umcp.unity-mcp-server  
**配布方式:** Unity Package (.unitypackage / Git URL)  
**ライセンス:** MIT License

## 主要アーキテクチャコンポーネント

### MCPサーバーコア
- **UMcpServer.cs**: HTTPサーバー (localhost:49001/umcp/) でJSON-RPC 2.0プロトコルを使用
- **UMcpSettings.cs**: ProjectSettings内のScriptableSingletonパターンによる設定管理
- **UMcpServerManager.cs**: Unity Editor統合、自動初期化、アセンブリリロード処理

### ビルトインツールシステム（4カテゴリ）
- **UnityInfoTool**: Unity情報とシーン構造の分析
- **AssetManagementTool**: アセット操作（リフレッシュ、検索、情報取得、再インポート、保存）
- **ConsoleLogTool**: コンソールログ管理（取得、クリア、統計、出力）
- **TestRunnerTool**: テスト実行（EditMode/PlayModeテスト、結果取得）

### カスタムツールフレームワーク
- **UMcpToolBuilder.cs**: 拡張可能なツール作成基底クラス
- **属性ベース登録**: `[McpServerToolType]`、`[McpServerTool]`による自動認識
- **ScriptableObject統合**: Unity標準のアセット管理システムとの統合

### Unity統合機能
- **uMCP.Editor.asmdef**: UniTask、ModelContextProtocol依存のEditorアセンブリ
- **メインスレッド同期**: すべてのUnity API呼び出しで`UniTask.SwitchToMainThread()`を使用
- **メニュー統合**: `Tools > uMCP`でのサーバー管理とツールアセット作成

## 開発パターン

### MCP設定管理
- 設定は`ProjectSettings/uMcpSettings.asset`に保存
- デフォルトサーバー: `localhost:49001/umcp/`
- 設定項目: IPアドレス、ポート、パス、CORS、デバッグモード、自動起動
- Unity Project Settings UIから直接アクセス可能
- 設定変更時の自動サーバー再起動

### ツール開発パターン
- **ScriptableObjectベース**: `UMcpToolBuilder`を継承したアセット作成
- **属性ベース実装**: `[McpServerToolType]`でクラス、`[McpServerTool]`でメソッドを定義
- **自動登録**: プロジェクト内のツールアセットを自動検出・読み込み
- **依存性注入**: `IServiceCollection`による柔軟なサービス登録

### 非同期処理パターン
- **UniTask統合**: Unity最適化された非同期処理
- **メインスレッド同期**: 全Unity API呼び出しで`await UniTask.SwitchToMainThread()`
- **適切なキャンセル処理**: CancellationTokenを使用したリソース管理
- **タイムアウト対応**: 設定可能なタイムアウト値による安全な処理

## 主要な依存関係

### 必須Unityパッケージ
- **UniTask (2.3.3+)**: 非同期処理とメインスレッド同期
- **Unity Test Framework**: テスト実行機能
- **Unity Editor標準API**: アセット管理、コンソール、シーン操作

### 組み込みNuGetパッケージ
- **ModelContextProtocol (0.3.0+)**: MCPサーバー実装のコア
- **Microsoft.Extensions.DependencyInjection**: 依存性注入システム
- **System.IO.Pipelines**: 高性能I/O処理

## MCPサーバー機能

### ビルトインツールセット（全18ツール）

#### Unity情報ツール
- **get_unity_info**: Unity エディターとプロジェクトの詳細情報
- **get_scene_info**: 現在のシーン構造とGameObject分析
- **log_message**: Unity コンソールへのログ出力

#### アセット管理ツール  
- **refresh_assets**: アセットデータベースのリフレッシュ
- **save_project**: プロジェクトとアセットの保存
- **find_assets**: フィルターによるアセット検索
- **get_asset_info**: アセットの詳細情報取得
- **reimport_asset**: 指定アセットの強制再インポート

#### コンソールログツール
- **get_console_logs**: Unity コンソールログの取得とフィルタリング
- **clear_console_logs**: コンソールログの全クリア
- **log_to_console**: カスタムメッセージのコンソール出力
- **get_log_statistics**: ログ統計情報の取得

#### テスト実行ツール（高度なPlayMode最適化搭載）
- **run_edit_mode_tests**: EditModeテストの実行と結果取得（標準実行）
- **run_play_mode_tests**: PlayModeテストの高速実行（ドメインリロード制御付き）
- **get_available_tests**: 利用可能なテスト一覧の取得（モード別フィルタリング対応）

**PlayModeテスト技術実装:**
- `EditorSettings.enterPlayModeOptionsEnabled`と`EnterPlayModeOptions`の制御
- `DisableDomainReload | DisableSceneReload`フラグの適用
- テスト実行前後での設定の保存・復元パターン
- `finally`ブロックでの確実な設定リストア
- コンパイル状態とPlay Mode状態の事前チェック

### 高度な通信機能
- **JSON-RPC 2.0準拠**: 完全なMCPプロトコル実装
- **HTTP/1.1サーバー**: 堅牢なHTTPリクエスト処理
- **CORS対応**: Web基盤MCPクライアント対応
- **エラーハンドリング**: 適切なHTTPステータスコードとエラーレスポンス
- **タイムアウト処理**: 設定可能なリクエストタイムアウト
- **デバッグモード**: 詳細なリクエスト/レスポンスログ

## パッケージ構造

**パッケージルート: `Assets/uMcp/` または `Packages/com.umcp.unity-mcp-server/`**

```
Assets/uMcp/
├── package.json                    # パッケージ定義
├── README.md                      # メインドキュメント
├── CHANGELOG.md                   # 変更履歴
├── LICENSE.md                     # MITライセンス
├── Editor/
│   ├── uMCP.Editor.asmdef        # エディターアセンブリ定義
│   ├── Core/                     # MCPサーバーコア実装
│   │   ├── UMcpServer.cs        # HTTPサーバー
│   │   ├── UMcpServerManager.cs # Unity統合・ライフサイクル
│   │   └── UMcpToolBuilder.cs   # ツール基底クラス
│   ├── Settings/
│   │   └── UMcpSettings.cs      # 設定管理
│   ├── Tools/                   # ビルトインツール実装
│   │   ├── UnityInfoTool.cs
│   │   ├── AssetManagementTool.cs
│   │   ├── ConsoleLogTool.cs
│   │   └── TestRunnerTool.cs
│   └── Attributes/              # カスタム属性
└── Samples~/                    # サンプルコード（オプション）
    └── BasicTools/
```

**重要な設定ファイル:**
- `/ProjectSettings/uMcpSettings.asset` - MCPサーバー設定（自動生成）
- `Tools > uMCP`メニューからの管理UI

## インストールと使用方法

### パッケージインストール
1. **Package Manager**: Git URL `https://github.com/your-repo/unity-mcp-server.git`
2. **Manual**: `Assets/uMcp/`を`Packages/com.umcp.unity-mcp-server/`にコピー

### 基本使用手順
1. **ツールアセット作成**: `Tools > uMCP > Create Default Tool Assets`
2. **サーバー管理**: 自動起動またはメニューから手動制御
3. **MCP接続**: `http://localhost:49001/umcp/`にMCPクライアント接続

## 開発上の重要事項

### パフォーマンス考慮
- **メインスレッド同期**: 全Unity API呼び出しで`await UniTask.SwitchToMainThread()`必須
- **適切なリソース管理**: アセンブリリロード時の自動クリーンアップ
- **タイムアウト処理**: 長時間実行の回避

### カスタムツール開発ガイドライン  
- `UMcpToolBuilder`継承でScriptableObject作成
- `[McpServerToolType]`でツールクラス定義
- `[McpServerTool]`で個別メソッド定義
- パラメータには`[Description]`属性を付与

### デバッグとトラブルシューティング
- **デバッグモード**: 設定でリクエスト/レスポンス詳細ログ有効化
- **サーバー状態確認**: `Tools > uMCP > Show Server Info`
- **ポート競合**: デフォルト49001番ポートが使用中の場合は設定変更

### PlayModeテスト実装ガイドライン

#### 必須実装パターン
1. **設定保存・復元パターン**: 
   ```csharp
   // メソッドレベルで変数宣言
   bool originalEnterPlayModeOptionsEnabled = false;
   EnterPlayModeOptions originalEnterPlayModeOptions = EnterPlayModeOptions.None;
   
   // 設定変更
   if (disableDomainReload) {
       originalEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
       originalEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
       EditorSettings.enterPlayModeOptionsEnabled = true;
       EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
   }
   
   // finally句での確実な復元
   finally {
       if (testMode == TestMode.PlayMode && disableDomainReload) {
           EditorSettings.enterPlayModeOptionsEnabled = originalEnterPlayModeOptionsEnabled;
           EditorSettings.enterPlayModeOptions = originalEnterPlayModeOptions;
       }
   }
   ```

2. **事前状態チェック**: 
   - `EditorApplication.isCompiling`でコンパイル中チェック
   - `EditorApplication.isPlaying`でPlay Mode中チェック
   - 適切なエラーレスポンス返却

3. **非同期処理統合**:
   - `await UniTask.SwitchToMainThread()`でのメインスレッド同期
   - `CancellationToken`による適切なタイムアウト処理
   - `TestResultCollector`との連携

#### 開発時の注意点
- PlayModeテスト実行中は他のUnity操作が制限される
- 設定復元失敗時のfallback処理を実装
- デバッグログによる状態遷移の可視化
- テスト実行前の500ms待機による安定性確保

# important-instruction-reminders
このプロジェクトは完成したUnityパッケージです。以下の点を守ってください：

## 基本方針
- **必要最小限の変更のみ**: 要求されていない機能追加は行わない
- **既存ファイル優先**: 新規ファイル作成より既存ファイルの編集を優先
- **ドキュメント作成制限**: 明示的に要求されない限りドキュメントファイル(.md)は作成しない

## CLAUDE.md更新方針
- **技術的実装詳細に集中**: パフォーマンス測定結果ではなく、実装パターンとコード例を記載
- **開発指針を優先**: 具体的な数値よりも、開発者が従うべき技術的ガイドラインを重視
- **実装パターンの文書化**: 成功した技術的解決策は必須実装パターンとして記録
- **コード例の提供**: 重要な実装については具体的なコード例を含める
- **開発時の注意点**: 実装時に遭遇する技術的な課題と対策を明記
- **アーキテクチャ整合性**: 既存のパッケージ設計パターンとの一貫性を保持

### ドキュメント役割分担
- **CLAUDE.md**: 開発者向け技術指針・実装パターン・アーキテクチャ詳細
- **README.md**: ユーザー向け機能説明・パフォーマンス比較・使用方法・トラブルシューティング
- **更新時の注意**: 技術的な成果はCLAUDE.mdに実装詳細を、README.mdにユーザーメリットを記載

## パッケージ整合性
- **package.json維持**: バージョン、依存関係、メタデータの整合性を保つ
- **アセンブリ定義保護**: uMCP.Editor.asmdefの設定を変更しない
- **ディレクトリ構造保持**: 確立されたパッケージ構造を維持

## コード品質
- **既存パターン準拠**: UMcpToolBuilder、属性ベース登録の既存パターンに従う
- **Unity API適切使用**: UniTask.SwitchToMainThread()の適切な使用
- **エラーハンドリング**: 既存の堅牢なエラー処理パターンを維持
