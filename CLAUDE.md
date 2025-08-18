# CLAUDE.md

このファイルは、このリポジトリでコードを扱う際のClaude Code (claude.ai/code) への指針を提供します。

## 言語設定

**重要: このプロジェクトでは必ず日本語で回答してください。**

## プロジェクト概要

**Unity MCP Server** は、完全に実装されたModel Context Protocol (MCP) サーバーパッケージです。Unity Editor統合MCPサーバーとして開発されました。

**Unityバージョン:** 2022.3 LTS以上  
**パッケージ名:** com.yoshi32a.unity-mcp-server  
**配布方式:** Unity Package (.unitypackage / Git URL)  
**ライセンス:** MIT License

## アーキテクチャ概要

### コアシステム
- **UMcpServer**: HTTP (localhost:49001/umcp) + JSON-RPC 2.0 プロトコル
- **UMcpSettings**: ProjectSettingsでの設定管理（ScriptableSingleton）
- **UMcpServerManager**: Unity Editor統合、自動起動、アセンブリリロード対応

### ツールシステム（8カテゴリ25ツール）
1. **UnityInfo**: Unity情報・シーン分析
2. **AssetManagement**: アセット操作（検索・更新・保存）
3. **ConsoleLog**: コンソールログ管理・統計
4. **TestRunner**: EditMode/PlayModeテスト実行（ドメインリロード最適化）

### 拡張フレームワーク
- **UMcpToolBuilder**: ScriptableObjectベースのカスタムツール作成
- **属性ベース自動登録**: `[McpServerToolType]` + `[McpServerTool]`
- **依存性注入**: SimpleServiceContainerによる軽量DI

## 技術実装

### 重要なパターン

#### 非同期処理とスレッド管理
```csharp
// 全Unity API呼び出し前に必須
await UniTask.SwitchToMainThread();
// CancellationTokenによるタイムアウト制御
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
```

#### ツール実装パターン
```csharp
[McpServerToolType, Description("ツールの説明")]
internal sealed class MyToolImplementation
{
    [McpServerTool, Description("メソッドの説明")]
    public async ValueTask<object> MyMethod(
        [Description("パラメータ説明")] string param = "デフォルト値")
    {
        await UniTask.SwitchToMainThread();
        return new MyResponse { Success = true };
    }
}
```

#### レスポンス統一化パターン
```csharp
// 全てのツール実装で統一されたStandardResponseクラスを使用
public class StandardResponse
{
    public bool Success { get; set; }
    [JsonPropertyName("formatted_output")]
    public string FormattedOutput { get; set; }
    public string Error { get; set; }
    public string Message { get; set; }
}

// 正しい実装例（StandardResponse使用）
return new StandardResponse
{
    Success = true,
    FormattedOutput = info.ToString()
};

// ❌ 避けるべき実装（無名クラス）
return new { Success = true, FormattedOutput = info.ToString() };
```

**レスポンス統一化の利点:**
- **型安全性**: コンパイル時の型チェック
- **JSONシリアライゼーション**: System.Text.Jsonとの確実な互換性
- **保守性**: 統一されたレスポンス構造による開発効率向上
- **MCPプロトコル適合**: 一貫したAPIレスポンス形式

### 主要依存関係
- **UniTask (2.3.3+)**: 非同期処理・メインスレッド同期
- **System.Text.Json (9.0.7+)**: 独自MCPプロトコル実装
- **Unity Test Framework**: テスト実行機能

#### ドキュメント検索最適化パターン
```csharp
// 並列インデックス化による高速検索実装
// 1. 並列事前インデックス構築
cachedIndex = await ParallelIndexBuilder.BuildOrUpdateIndexAsync(DocumentationPath);

// 2. キーワードベース高速検索
if (cachedIndex.KeywordIndex.TryGetValue(queryTerm, out var exactMatches))
{
    foreach (var entryIndex in exactMatches)
    {
        candidateScores[entryIndex] = candidateScores.GetValueOrDefault(entryIndex, 0) + 5.0f;
    }
}

// 3. 結果スコアリングとソート
var sortedCandidates = candidateScores
    .OrderByDescending(kvp => kvp.Value)
    .Take(maxResults * 2);
```

**パフォーマンス最適化技術:**
- **並列事前インデックス化**: HTMLファイルを並列処理で事前解析してキーワードマップ作成（最大8並列）
- **キーワードマップ検索**: O(1)でのキーワードマッチング
- **スコアベースランキング**: 完全一致5.0、部分一致2.0、タイトル一致10.0のスコアリング
- **インデックスキャッシング**: 7日間有効な永続化インデックス
- **自動インデックス更新**: Unity バージョン変更時の自動再構築

## MCPサーバー機能

### ビルトインツールセット（全26ツール）

#### Unity情報ツール（6ツール）
- **get_unity_info**: Unity エディターとプロジェクトの詳細情報
- **get_scene_info**: 現在のシーン構造とGameObject分析
- **get_hierarchy_analysis**: 指定GameObjectとその子階層の構造を詳細分析
- **get_game_object_info**: 指定GameObjectの詳細情報を取得
- **get_prefab_info**: 指定Prefabの詳細情報を取得
- **detect_missing_scripts**: シーン内のMissing Script（nullコンポーネント）を検出

#### アセット管理ツール（4ツール）
- **refresh_assets**: アセットデータベースのリフレッシュ
- **save_project**: プロジェクトとアセットの保存
- **find_assets**: フィルターによるアセット検索
- **get_asset_info**: アセットの詳細情報取得

#### コンソールログツール（4ツール）
- **get_console_logs**: Unity コンソールログの取得とフィルタリング
- **clear_console_logs**: コンソールログの全クリア
- **log_to_console**: カスタムメッセージのコンソール出力
- **get_log_statistics**: ログ統計情報の取得

#### テスト実行ツール（3ツール）
- **run_edit_mode_tests**: EditModeテストの実行と結果取得（標準実行）
- **run_play_mode_tests**: PlayModeテストの実行（ドメインリロード制御付き）
- **get_available_tests**: 利用可能なテスト一覧の取得（モード別フィルタリング対応）

#### エディタ拡張ツール（1ツール）
- **execute_editor_method**: コンパイル済みエディタ拡張の静的メソッドを実行

#### ドキュメント検索ツール（2ツール）
- **search_documentation**: Unity公式ドキュメント（Manual/ScriptReference）の検索
- **rebuild_documentation_index**: ドキュメントインデックスの再構築

#### ビルド管理ツール（4ツール）
- **get_build_status**: 現在のビルド状態と最後のビルド結果を取得
- **wait_for_build_completion**: ビルドの完了を待機して結果を返す
- **get_last_build_log**: 最後のビルドの詳細ログを取得
- **clear_build_cache**: ビルドキャッシュをクリアして次回フルビルドを強制

#### ワークフロー提案ツール（2ツール）
- **get_next_action_suggestions**: 現在の状態から推奨される次のMCPツール実行を提案
- **get_workflow_patterns**: Markdownファイルから読み込んだワークフローパターンを取得

**ビルド管理技術実装:**
- **静的ビルドレポート管理**: `BuildReport lastBuildReport`でビルド結果の永続化
- **ビルド状態トラッキング**: `bool isBuildInProgress`でリアルタイム状態監視
- **プラットフォーム横断対応**: 全ビルドターゲット（Windows, Mac, Linux, Mobile等）対応
- **キャッシュ管理**: `Library/BuildCache`と`Library/il2cpp_cache`の自動クリア機能
- **詳細ログ出力**: ビルドステップ、エラー、警告の構造化レポート生成

**PlayModeテスト技術実装:**
- `EditorSettings.enterPlayModeOptionsEnabled`と`EnterPlayModeOptions`の制御
- `DisableDomainReload | DisableSceneReload`フラグの適用
- テスト実行前後での設定の保存・復元パターン
- `finally`ブロックでの確実な設定リストア
- コンパイル状態とPlay Mode状態の事前チェック

### Markdownワークフローシステム
**コンテキスト対応のワークフロー提案**
- **Markdownベース定義**: 開発者が簡単に編集可能な`.md`ファイルでワークフロー定義
- **動的提案システム**: 実行したツールと作業コンテキストに基づく次アクション提案
- **4つの組み込みワークフロー**: エディタ拡張開発、エラー調査、テスト実行、アセット管理
- **トリガーシステム**: ツール実行後の自動推奨とキーワードベースマッチング
- **パラメータ付き実行**: 各ステップに適切なパラメータを自動設定

**実装技術:**
- `WorkflowMarkdownParser`: .mdファイルの構造化パース
- `ToolWorkflowSuggestionImplementation`: コンテキスト分析とマッチング
- `Assets/uMcp/Workflows/`: Markdownワークフロー定義ディレクトリ

### 高度な通信機能
- **JSON-RPC 2.0準拠**: 完全なMCPプロトコル実装
- **HTTP/1.1サーバー**: 堅牢なHTTPリクエスト処理
- **CORS対応**: Web基盤MCPクライアント対応
- **エラーハンドリング**: 適切なHTTPステータスコードとエラーレスポンス
- **タイムアウト処理**: 設定可能なリクエストタイムアウト
- **デバッグモード**: 詳細なリクエスト/レスポンスログ

## パッケージ構造

**パッケージルート: `Assets/uMcp/` または `Packages/com.yoshi32a.unity-mcp-server/`**

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
│   ├── Tools/                   # ビルトインツール実装（25ツール）
│   │   ├── UnityInfo/           # Unity情報ツール（5ツール）
│   │   ├── AssetManagement/     # アセット管理ツール（4ツール）
│   │   ├── ConsoleLog/          # コンソールログツール（4ツール）
│   │   ├── TestRunner/          # テスト実行ツール（3ツール）
│   │   ├── EditorExtension/     # エディタ拡張ツール（1ツール）
│   │   └── ToolWorkflow/        # ワークフロー提案ツール（2ツール）
│   └── Attributes/              # カスタム属性
├── Workflows/                   # Markdownワークフロー定義
│   ├── editor-extension-workflow.md
│   ├── error-investigation-workflow.md
│   └── workflow-triggers.md
└── Samples~/                    # サンプルコード（オプション）
    └── BasicTools/
```

**重要な設定ファイル:**
- `/ProjectSettings/uMcpSettings.asset` - MCPサーバー設定（自動生成）
- `Tools > uMCP`メニューからの管理UI

## インストールと使用方法

### パッケージインストール
1. **Package Manager**: Git URL `https://github.com/yoshi32a/uMcp.git?path=Assets/uMcp`
2. **Manual**: `Assets/uMcp/`を`Packages/com.yoshi32a.unity-mcp-server/`にコピー

### 基本使用手順
1. **ツールアセット作成**: `Tools > uMCP > Create Default Tool Assets`
2. **サーバー管理**: 自動起動またはメニューから手動制御
3. **MCP接続**: `http://localhost:49001/umcp/`にMCPクライアント接続

## 開発上の重要事項

### Componentのnull参照対策パターン
Unity APIの`GetComponents<Component>()`使用時には必ずnullチェックを実装すること。

#### 問題の背景
Missing Script（削除されたスクリプト、コンパイルエラー等）が存在する場合、GetComponentsは配列内にnull要素を含む。

#### 必須実装パターン
```csharp
// ❌ 危険な実装（null参照エラーの可能性）
var components = gameObject.GetComponents<Component>();
info.AppendLine($"コンポーネント数: {components.Length}");

// ✅ 安全な実装（nullフィルタリング）
var components = gameObject.GetComponents<Component>();
var validComponents = components.Where(c => c != null).ToArray();
info.AppendLine($"コンポーネント数: {validComponents.Length}");
```

#### 適用箇所
- シーン情報取得時のコンポーネント数カウント
- GameObject詳細情報の処理
- Prefab情報の取得
- UI要素の分析
- Missing Script検出ツール

### パフォーマンス考慮
- **メインスレッド同期**: 全Unity API呼び出しで`await UniTask.SwitchToMainThread()`必須
- **リソース管理**: アセンブリリロード時の自動クリーンアップ
- **タイムアウト処理**: 長時間実行の回避

### ビルトインツール登録ガイドライン
- **UMcpServer.csでの登録必須**: 新しいツール実装クラスは`UMcpServer.cs`のビルトインツールリストに手動追加が必要
- **登録忘れ防止**: ツール作成後は必ずMCPサーバー再起動してツール一覧に表示されることを確認
- **静的インスタンス管理**: ツール実装クラスは静的フィールドでBuildReportなどの状態管理を行う
- **適切なライフサイクル**: アセンブリリロード時の状態リセットを考慮した設計

### カスタムツール開発ガイドライン  
- `UMcpToolBuilder`継承でScriptableObject作成
- `[McpServerToolType]`でツールクラス定義
- `[McpServerTool]`で個別メソッド定義
- パラメータには`[Description]`属性を付与

### デバッグとトラブルシューティング
- **デバッグモード**: 設定でリクエスト/レスポンス詳細ログ有効化
- **サーバー状態確認**: `Tools > uMCP > Show Server Info`
- **ポート競合**: デフォルト49001番ポートが使用中の場合は設定変更

### リリース管理とバージョニング

**重要：新バージョンリリース時は必ずGitタグを作成すること**

#### リリース手順（必須）
1. **バージョン更新**：
   ```bash
   # package.jsonのバージョンを更新 (例: "1.0.1" -> "1.0.2")
   # CHANGELOG.mdに新バージョンエントリを追加
   ```

2. **変更をコミット**：
   ```bash
   git add Assets/uMcp/package.json Assets/uMcp/CHANGELOG.md
   git commit -m "release: v1.0.2 - 変更内容の要約"
   git push
   ```

3. **タグ作成（必須）**：
   ```bash
   git tag v1.0.2 -m "Release v1.0.2: 変更内容の詳細説明"
   git push origin v1.0.2
   ```

**タグ作成の重要性:**
- **Unity Package Manager**: 特定バージョン指定インストール `#v1.0.2`
- **リリース履歴**: GitHub Releasesページでの正式リリース管理
- **後方互換性**: 旧バージョンへの安全な戻し
- **セマンティックバージョニング**: patch/minor/majorバージョン管理

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
- **1クラス1ファイル原則**: 1つのファイルには1つのpublicクラスのみを定義、内部クラスはprivateまたはファイル内でのみ使用される場合のみ許可

## ドキュメント品質ガイドライン
- **曖昧な修飾語の禁止**: 「最適化された」「強化された」「インテリジェントな」「スマートな」「包括的な」「完全な」「簡単に」「高速な」等の曖昧な表現を使用しない
- **具体的な記述**: 機能の動作、対象範囲、実行条件、期待結果を明確に記述
- **履歴情報の分離**: バグ修正履歴や「新機能」等の時期依存表現はREADMEに記載せず、CHANGELOGで管理
- **技術的正確性**: 実際のツール数、機能、制限事項を正確に記載
- **ユーザー視点**: ユーザーが理解しやすい明確で簡潔な表現を使用
