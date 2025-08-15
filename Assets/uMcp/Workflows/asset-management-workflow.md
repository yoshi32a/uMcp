# アセット管理ワークフロー

## 概要
Unity プロジェクトのアセットを効率的に管理・最適化するワークフロー

## タグ
- アセット管理
- 最適化
- メンテナンス
- インポート
- 整理

## ステップ

### 1. アセット検索・分析
- tool: find_assets
- 説明: プロジェクト内のアセットを検索して現状を把握
- パラメータ:
  - maxResults: 100
- 必須: true

### 2. 問題アセット特定
- tool: get_console_logs
- 説明: アセットインポート時のエラーや警告を確認
- パラメータ:
  - includeWarnings: true
  - errorsOnly: false
  - maxLogs: 50
- 必須: true

### 3. アセット詳細確認
- tool: get_asset_info
- 説明: 特定アセットの詳細情報を取得
- パラメータ:
  - assetPath: (ユーザー指定)
- 必須: false
- 条件: 問題のあるアセットが特定された場合


### 4. アセットデータベース更新
- tool: refresh_assets
- 説明: 全体的なアセットデータベースの更新
- 必須: true

### 5. プロジェクト保存
- tool: save_project
- 説明: アセット管理作業の完了後にプロジェクトを保存
- 必須: true

### 6. 結果確認
- tool: get_console_logs
- 説明: 管理作業後のエラー状況を最終確認
- パラメータ:
  - errorsOnly: true
  - maxLogs: 10
- 必須: true

## 関連ワークフロー
- error-investigation-workflow.md
- test-execution-workflow.md
