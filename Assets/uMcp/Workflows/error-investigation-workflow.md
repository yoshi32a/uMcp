# エラー調査ワークフロー

## 概要
エラーが発生した際の体系的な調査手順

## タグ
- デバッグ
- エラー調査
- トラブルシューティング

## トリガー条件
- lastTool の結果にエラーが含まれる
- context に "error" または "エラー" が含まれる
- console_logs でエラーが検出された

## ステップ

### 1. エラーログ取得
- tool: get_console_logs
- 説明: エラーの詳細を取得
- パラメータ:
  - errorsOnly: true
  - maxLogs: 20
  - maxMessageLength: 1000
- 必須: true

### 2. Unity環境情報
- tool: get_unity_info
- 説明: 実行環境の情報を確認
- 必須: true

### 3. シーン状態確認
- tool: get_scene_info
- 説明: シーンの状態とGameObjectを確認
- 必須: false
- 条件: シーン関連のエラーの場合

### 4. コンソールクリア（オプション）
- tool: clear_console_logs
- 説明: 調査後にコンソールをクリア
- 必須: false
- 確認: ユーザーに確認を求める

## 推奨される次のアクション
- 問題が解決した場合: save_project
- 追加調査が必要な場合: get_hierarchy_analysis
- コードの修正が必要な場合: refresh_assets → execute_editor_method