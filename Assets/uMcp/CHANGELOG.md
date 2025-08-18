# 変更履歴

Unity MCP Serverのすべての重要な変更はこのファイルに記録されます。

形式は [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) に基づいており、
このプロジェクトは [セマンティック バージョニング](https://semver.org/spec/v2.0.0.html) に準拠しています。

## [1.0.8] - 2025-08-18

### Fixed
- **不要なUniTaskインスタンス削除**: コード品質向上のため不要な`new UniTask`を削除

## [1.0.7] - 2025-08-18

### Removed
- **GetPrefabInfo削除**: GetAssetInfoとの機能重複を解消してツール統合
  - GetPrefabInfoメソッドを削除（AssetManagementから）
  - Prefab解析機能をGetAssetInfoに統合（UnityInfoへ移動）
  - 重複コードの除去により保守性向上

### Changed
- **GetAssetInfo統合**: アセット情報取得機能を一元化
  - AssetManagementからUnityInfoカテゴリに移動
  - Prefabと一般アセット両方を統一インターフェースで処理
  - 再帰的な依存関係表示とPrefab階層表示を統合

### Improved
- **ツール数削減**: 25→24ツールに整理
  - コードベース簡素化と重複機能除去
  - Unity情報カテゴリ: 5→4ツール
  - 統一されたアセット解析フローの確立

## [1.0.6] - 2025-08-19

### Removed
- **GetHierarchyAnalysis削除**: 機能重複によりコードベース大幅簡素化
  - GetHierarchyAnalysisメソッド（特定オブジェクト分析機能）
  - 関連する未使用分析メソッド群（AnalyzeUIElements, AnalyzePerformance, AnalyzeDesign等）
  - 関連クラス定義（HierarchyAnalysis, HierarchyNode, UIAnalysis）

### Improved  
- **GetSceneInfo強化**: 完全な階層構造表示機能を統合
  - 再帰的な子オブジェクト表示（ツリー構造の視覚化）
  - 統計情報追加（総オブジェクト数、最大階層深度、アクティブ状態カウント）
  - アクティブ状態アイコンとコンポーネント情報表示
- **コードベース簡素化**: ツール数26→25に整理、保守性向上

## [1.0.5] - 2025-08-18

### Removed
- **不要なレスポンスクラス削除**: コードベースの簡素化
  - AssetManagement関連の専用レスポンスクラス（AssetInfo, AssetInfoResponse, AssetOperationResponse, AssetSearchResponse）
  - ConsoleLog関連の専用レスポンスクラス（LogStatisticsResponse, LogSummary, LogToConsoleResponse, StatisticsSummary）
  - StandardResponseクラスで統一され、型安全性とJSON互換性が向上

### Improved
- **コードベース簡素化**: 重複したレスポンスクラスの削除により保守性向上
- **統一されたレスポンス形式**: StandardResponseによる一貫したAPI設計

## [1.0.4] - 2025-08-18

### Added
- **Missing Script検出ツール**: `detect_missing_scripts` - シーン内のnullコンポーネントを持つGameObjectを検出
  - 全シーン/アクティブのみ/非アクティブのみの検索範囲指定
  - 詳細な統計情報（問題GameObject数、Missing Script総数、平均Missing数）
  - 階層パスと有効コンポーネント一覧の表示
  - 推奨アクションの提示

### Fixed
- **Componentのnull参照対策**: UnityInfoツール全体でnullチェックを実装
  - GetComponents<Component>()で取得した配列のnull要素を適切にフィルタリング
  - Missing Scriptが存在してもツールがエラーなく動作するよう改善
  - コンポーネント数のカウントを有効なコンポーネントのみで行うよう修正

### Improved
- **コード品質**: Componentのnullチェックパターンを統一
  - `Where(c => c != null)`による一貫したフィルタリング実装
  - validComponentsとして有効なコンポーネントのみを処理するパターンの確立

## [1.0.3] - 2025-08-15

### Improved
- **ドキュメント品質向上**: 全ドキュメントで情報整合性と表現の明確化を実施
- **正確な情報**: ツール数を21から正確な25に修正
- **表現の明確化**: 曖昧な修飾語（「インテリジェント」「強力な」「完全な」等）を削除
- **情報整理**: 履歴情報とバグ修正記録をCHANGELOGに適切に分離
- **パッケージ情報統一**: 全ドキュメントでパッケージ名とリポジトリURLを統一

### Technical
- **ドキュメント構造最適化**: README.mdから不要なリポジトリ構造セクションを削除
- **メタデータ整合性**: package.json、CHANGELOG.md、CLAUDE.md、README.md間の情報一致

## [1.0.2] - 2025-08-15

### Changed
- **ドキュメント国際化**: プロジェクト全体のドキュメントを英語から日本語に統一
- **package.json**: description フィールドを日本語に変更、ツール数を21から25に修正
- **LICENSE.md**: MIT ライセンス全体を日本語版に変換
- **CHANGELOG.md**: 全ての英語コンテンツを日本語に変換
- **ワークフロー**: documentation-search-workflow.md タイトルを日本語に変更

### Improved
- **ユーザビリティ**: 日本語ユーザー向けの完全な日本語ドキュメント環境を提供
- **保守性**: 一貫した言語によるドキュメント管理を実現

## [1.0.1] - 2025-08-15

### 修正
- **レスポンス標準化**: 全ツール実装で無名クラスを `StandardResponse` クラスに置き換え
- **型安全性**: 無名オブジェクト戻り値を削除してコンパイル時型チェックを改善
- **JSON シリアライゼーション**: 一貫したレスポンス構造でSystem.Text.Json互換性を強化
- **コード一貫性**: 24個のビルトインツール全体でレスポンス形式を統一
- **ビルド管理ツール**: 4つの新しいビルド関連ツールを追加

### 技術的改善
- MCPプロトコル準拠のために適切なJSONプロパティ命名を持つ `StandardResponse` クラスを追加
- Vector/Color無名オブジェクトを可読性向上のためにフォーマットされた文字列表現に変換
- 標準化されたレスポンスパターンによる保守性の向上
- 不正な無名クラス置換に関連するコンパイルエラーを修正

## [1.0.0] - 2024-07-30

### 追加
- Unity MCP Serverの初回リリース
- HTTP転送を使用したコアMCPサーバー実装
- Unity Editorと統合された自動起動機能
- 8つのビルトインツールカテゴリを持つツールシステム

#### コア機能
- **HTTPサーバー**: 設定可能なポート上で動作（デフォルト: 49001）
- **JSON-RPC 2.0**: 完全なModel Context Protocol準拠
- **リアルタイム通信**: Unity Editorの直接統合
- **エラーハンドリング**: タイムアウトサポート付きの堅牢なエラー処理
- **CORS サポート**: Webベースクライアント用の設定可能なCORS

#### ビルトインツール（24ツール）
- **Unity情報ツール（5ツール）**
  - `get_unity_info`: Unity エディターとプロジェクト情報
  - `get_scene_info`: 現在のシーン構造分析
  - `detect_missing_scripts`: Missing Script検出
  - `get_game_object_info`: 指定GameObjectの詳細情報


- **アセット管理ツール（4ツール）**
  - `refresh_assets`: アセットデータベースのリフレッシュ
  - `save_project`: プロジェクトとアセットの保存
  - `find_assets`: フィルタリング付きアセット検索
  - `get_asset_info`: 詳細なアセット情報

- **ビルド管理ツール（4ツール）**
  - `get_build_status`: 現在のビルド状態と最後のビルド結果
  - `wait_for_build_completion`: ビルド完了待機
  - `get_last_build_log`: 最後のビルドの詳細ログ
  - `clear_build_cache`: ビルドキャッシュクリア

- **コンソールログツール（4ツール）**
  - `get_console_logs`: フィルタリング付きコンソールログ取得
  - `clear_console_logs`: すべてのコンソールログのクリア
  - `log_to_console`: カスタムコンソール出力
  - `get_log_statistics`: コンソールログ統計

- **テスト実行ツール（3ツール）**
  - `run_edit_mode_tests`: EditModeテストの実行
  - `run_play_mode_tests`: PlayModeテストの実行
  - `get_available_tests`: 利用可能なテストの一覧

- **ドキュメント検索ツール（2ツール）**
  - `search_documentation`: Unity公式ドキュメント検索
  - `rebuild_documentation_index`: ドキュメントインデックス再構築

- **ワークフロー提案ツール（2ツール）**
  - `get_next_action_suggestions`: 次のアクション提案
  - `get_workflow_patterns`: Markdownワークフローパターン

- **エディタ拡張ツール（1ツール）**
  - `execute_editor_method`: 静的メソッド実行

#### 設定と管理
- **Unity メニュー統合**: `Tools > uMCP` メニュー
- **設定管理**: ProjectSettings統合
- **ツールアセット作成**: 自動化されたツールアセット管理
- **デバッグモード**: 詳細なリクエスト/レスポンスログ

#### 開発者エクスペリエンス
- **カスタムツールフレームワーク**: 拡張可能なツール作成システム
- **ScriptableObject統合**: Unity ネイティブツール設定
- **自動検出**: 動的ツール読み込み
- **アセンブリリロード処理**: コード変更時の適切なクリーンアップ

### 技術詳細
- **Unity バージョン**: Unity 2022.3 LTS 以降が必要
- **依存関係**: UniTask 2.3.3+
- **アーキテクチャ**: 関心の明確な分離を持つモジュラー設計
- **パフォーマンス**: Unity Editor のパフォーマンス最適化
- **セキュリティ**: 設定可能なアクセスを持つローカル専用サーバー

### ドキュメント
- クイックスタートガイド付きの包括的なREADME
- すべてのツールのAPIドキュメント
- アーキテクチャ概要
- トラブルシューティングガイド
- カスタムツール開発例

## [未リリース]

### 予定機能
- Package Manager統合ツール
- パフォーマンスプロファイリングツール
- カスタムインスペクター統合

---

## 開発ノート

Unity開発ワークフロー向けの包括的でプロダクション対応のMCPサーバー実装を提供することを目的としています。

### アーキテクチャの決定
- **HTTP over Stdio**: より良いデバッグとクライアント互換性のために選択
- **ScriptableObject ツール**: ツール設定のためのUnityネイティブアプローチ
- **UniTask 統合**: Unity向けに最適化されたasync/awaitサポート
- **モジュラー設計**: 拡張と保守が容易

### パフォーマンスの考慮事項
- **メインスレッド切り替え**: すべてのUnity API呼び出しを適切にマーシャリング
- **リソース管理**: サーバーリソースの適切な破棄
- **アセンブリリロード**: コード再コンパイルの優雅な処理
- **エラー復旧**: エディタークラッシュなしの堅牢なエラーハンドリング