# ワークフロートリガー定義

## 自動トリガールール

### ツールベーストリガー

#### refresh_assets 実行後
- 推奨: execute_editor_method (高)
- 推奨: get_console_logs (中) - エラーチェック用
- 条件: 新しい .cs ファイルが検出された場合

#### execute_editor_method 実行後
- 推奨: get_console_logs (高) - 実行結果確認
- 条件: 常に

#### save_project 実行後
- 推奨: refresh_assets (低)
- 条件: ファイル変更がある場合

#### run_edit_mode_tests / run_play_mode_tests 実行後
- 推奨: get_console_logs (高) - テスト結果確認
- 条件: テストが失敗した場合

#### get_build_status 実行後
- 推奨: get_last_build_log (中) - ビルド詳細確認
- 推奨: wait_for_build_completion (中) - ビルド進行中の場合
- 条件: ビルドエラーが検出された場合

#### clear_build_cache 実行後
- 推奨: wait_for_build_completion (高) - 次回ビルド完了待機
- 推奨: get_build_status (中) - キャッシュクリア効果確認
- 条件: 常に

### コンテキストベーストリガー

#### キーワード: "test", "テスト"
- 推奨ワークフロー: test-execution-workflow.md
- ツール候補:
  - get_available_tests
  - run_edit_mode_tests
  - run_play_mode_tests

#### キーワード: "error", "エラー", "failed", "失敗"
- 推奨ワークフロー: error-investigation-workflow.md
- ツール候補:
  - get_console_logs (errorsOnly: true)
  - get_unity_info

#### キーワード: "asset", "アセット", "import"
- 推奨ワークフロー: asset-management-workflow.md
- ツール候補:
  - find_assets
  - get_asset_info
  - refresh_assets

#### キーワード: "scene", "シーン", "GameObject"
- ツール候補:
  - get_scene_info
  - get_hierarchy_analysis
  - get_game_object_info

#### キーワード: "documentation", "document", "ドキュメント", "Unity API", "Manual", "ScriptReference"
- 推奨ワークフロー: documentation-search-workflow.md
- ツール候補:
  - search_documentation
  - rebuild_documentation_index

#### キーワード: "search", "検索", "find API", "Unity help"
- 推奨ワークフロー: documentation-search-workflow.md
- ツール候補:
  - search_documentation
  - get_unity_info

#### キーワード: "build", "ビルド", "compile", "コンパイル"
- 推奨ワークフロー: build-management-workflow.md
- ツール候補:
  - get_build_status
  - get_last_build_log
  - wait_for_build_completion
  - clear_build_cache

## 連鎖ルール

### エラー発生時の自動連鎖
1. エラー検出 → get_console_logs (errorsOnly: true)
2. → get_unity_info
3. → (オプション) get_scene_info

### 新規ファイル作成時の自動連鎖
1. ファイル作成検出 → refresh_assets
2. → (待機: コンパイル完了)
3. → execute_editor_method または get_console_logs

### ドキュメント検索時の自動連鎖
1. 初回検索 → rebuild_documentation_index (インデックス未存在時)
2. → search_documentation
3. → (オプション) log_to_console (結果記録用)

### インデックス更新時の自動連鎖
1. Unity バージョン変更検出 → get_unity_info
2. → rebuild_documentation_index
3. → search_documentation (検証用)
4. → save_project