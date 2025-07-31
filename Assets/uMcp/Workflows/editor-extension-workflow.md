# エディタ拡張開発ワークフロー

## 概要
新しいエディタ拡張スクリプトを作成して実行するワークフロー

## タグ
- エディタ拡張
- スクリプト作成
- 開発

## ステップ

### 1. アセットリフレッシュ
- tool: refresh_assets
- 説明: 新しいスクリプトファイルをUnityに認識させる
- 必須: true

### 2. コンパイルエラー確認
- tool: get_console_logs
- 説明: コンパイルエラーがないか確認
- パラメータ:
  - errorsOnly: true
  - maxLogs: 10
- 必須: false
- 条件: エラーがある場合は修正が必要

### 3. メソッド実行
- tool: execute_editor_method
- 説明: 作成したエディタ拡張メソッドを実行
- パラメータ:
  - className: (ユーザー指定)
  - methodName: (ユーザー指定)
- 必須: true

### 4. 実行結果確認
- tool: get_console_logs
- 説明: メソッドの実行結果を確認
- パラメータ:
  - maxLogs: 5
  - errorsOnly: false
- 必須: true

## 関連ワークフロー
- error-investigation-workflow.md
- test-execution-workflow.md