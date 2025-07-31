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
  - reimport_asset

#### キーワード: "scene", "シーン", "GameObject"
- ツール候補:
  - get_scene_info
  - get_hierarchy_analysis
  - get_game_object_info

## 連鎖ルール

### エラー発生時の自動連鎖
1. エラー検出 → get_console_logs (errorsOnly: true)
2. → get_unity_info
3. → (オプション) get_scene_info

### 新規ファイル作成時の自動連鎖
1. ファイル作成検出 → refresh_assets
2. → (待機: コンパイル完了)
3. → execute_editor_method または get_console_logs