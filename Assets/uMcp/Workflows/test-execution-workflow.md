# テスト実行ワークフロー

## 概要
Unity プロジェクトでテストを体系的に実行するワークフロー

## タグ
- テスト
- 品質保証
- CI/CD
- EditMode
- PlayMode

## ステップ

### 1. 利用可能テスト確認
- tool: get_available_tests
- 説明: プロジェクト内のテスト一覧を取得して実行対象を把握
- パラメータ:
  - testMode: All
- 必須: true

### 2. EditModeテスト実行
- tool: run_edit_mode_tests
- 説明: エディターモードでのユニットテストを実行
- パラメータ:
  - timeoutSeconds: 300
- 必須: false
- 条件: EditModeテストが存在する場合

### 3. PlayModeテスト実行
- tool: run_play_mode_tests
- 説明: Play Modeでの統合テストを高速実行
- パラメータ:
  - disableDomainReload: true
  - timeoutSeconds: 600
- 必須: false
- 条件: PlayModeテストが存在する場合

### 4. テスト結果確認
- tool: get_console_logs
- 説明: テスト実行中のエラーや警告を確認
- パラメータ:
  - includeWarnings: true
  - maxLogs: 20
- 必須: true

### 5. プロジェクト保存
- tool: save_project
- 説明: テスト完了後にプロジェクト状態を保存
- 必須: true

## 関連ワークフロー
- error-investigation-workflow.md
- editor-extension-workflow.md