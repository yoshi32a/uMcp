# ビルド管理ワークフロー

## 概要
Unityプロジェクトのビルド状況監視と問題解決のワークフロー

## タグ
- ビルド
- コンパイル
- エラー調査
- パフォーマンス
- 最適化

## ワークフローパターン

### 1. ビルド状況確認

**トリガー:**
- 開発開始時の状況把握
- コード変更後の確認
- エラー調査の前段階

**実行手順:**
```
1. get_build_status
   - 現在のコンパイル状況を確認
   - 最後のビルド結果を取得
   - エラーや警告の有無を確認

2. get_console_logs
   - errorsOnly: true
   - maxLogs: 50
   - ビルド関連エラーを特定

3. get_unity_info
   - Unity バージョンとプロジェクト情報
   - ビルド環境の確認
```

### 2. ビルド完了待機・監視

**トリガー:**
- バッチビルド実行時
- 大規模な変更後のビルド
- CI/CD パイプライン内

**実行手順:**
```
1. wait_for_build_completion
   - timeoutSeconds: 600
   - ビルド完了まで待機
   - 進捗状況を監視

2. get_last_build_log
   - maxSteps: 100
   - 詳細なビルドログを取得
   - エラーと警告を分析

3. get_console_logs
   - includeWarnings: true
   - ビルド後の状態確認
```

### 3. ビルドエラー調査

**トリガー:**
- ビルド失敗時
- コンパイルエラー発生時
- 警告の大量発生時

**実行手順:**
```
1. get_build_status
   - 現在の状況を把握
   - 最後のビルド結果を確認

2. get_last_build_log
   - maxSteps: 50
   - 失敗したビルドステップを特定
   - エラーメッセージを詳細分析

3. search_documentation
   - ビルドエラーに関する公式情報検索
   - 解決策の調査

4. get_console_logs
   - errorsOnly: true
   - 関連するランタイムエラー確認

5. get_scene_info
   - シーンの状態確認
   - 問題のあるGameObjectを特定
```

### 4. ビルドパフォーマンス最適化

**トリガー:**
- ビルド時間の長期化
- キャッシュ問題の疑い
- プロジェクト肥大化時

**実行手順:**
```
1. get_build_status
   - 現在のビルド状況確認
   - 前回ビルド時間を記録

2. clear_build_cache
   - ビルドキャッシュをクリア
   - フルビルドを強制実行

3. wait_for_build_completion
   - timeoutSeconds: 900
   - キャッシュクリア後のビルド完了待機

4. get_last_build_log
   - ビルド時間とステップ詳細を確認
   - パフォーマンス改善効果を測定

5. save_project
   - 最適化後の状態を保存
```

### 5. 継続的ビルド監視

**トリガー:**
- CI/CD環境
- 夜間バッチ処理
- 定期的な品質チェック

**実行手順:**
```
1. get_available_tests
   - testMode: "All"
   - 利用可能なテスト確認

2. run_edit_mode_tests
   - categoryNames: "Build"
   - ビルド関連のテスト実行

3. get_build_status
   - テスト後のビルド状況確認

4. wait_for_build_completion
   - timeoutSeconds: 1800
   - 完全ビルドの完了待機

5. get_last_build_log
   - maxSteps: 200
   - 全ビルドプロセスの記録

6. log_to_console
   - ビルド監視結果の記録
   - 次回の参考情報として保存
```

## 推奨パラメータ

### get_build_status
- **パラメータ**: なし
- **実行頻度**: 開発セッション開始時、コード変更後

### wait_for_build_completion
- **短時間ビルド**: timeoutSeconds: 300
- **中規模ビルド**: timeoutSeconds: 600
- **大規模ビルド**: timeoutSeconds: 1200

### get_last_build_log
- **エラー調査**: maxSteps: 20-50
- **詳細分析**: maxSteps: 100+
- **フルログ**: maxSteps: 500

### clear_build_cache
- **実行タイミング**: エラー頻発時、週1回程度
- **注意**: 次回ビルドが大幅に遅くなる

## パフォーマンス指標

### 正常値
- **小規模変更**: ビルド時間 < 30秒
- **中規模変更**: ビルド時間 < 2分
- **フルビルド**: ビルド時間 < 5分
- **エラー数**: 0件
- **警告数**: < 10件

### 問題のサイン
- ビルド時間 > 10分 → キャッシュクリア検討
- エラー数 > 0 → 即座に調査必要
- 警告数 > 50 → コード品質確認必要

## トラブルシューティング

### よくある問題
1. **コンパイルエラー**
   - get_last_build_log でエラー詳細確認
   - search_documentation でUnity API確認
   - get_console_logs でランタイムエラー確認

2. **ビルド時間の長期化**
   - clear_build_cache でキャッシュリセット
   - get_build_status で進捗確認
   - アセット最適化検討

3. **メモリ不足**
   - get_unity_info でシステム情報確認
   - 不要アセットの削除検討
   - ビルド設定の見直し

### デバッグ手順
```
1. get_build_status → 現在状況把握
2. get_last_build_log → 問題箇所特定  
3. get_console_logs → 関連エラー確認
4. search_documentation → 解決策調査
5. clear_build_cache → 問題が解決しない場合
6. wait_for_build_completion → 最終確認
```

## 関連ワークフロー
- error-investigation-workflow.md
- test-execution-workflow.md
- asset-management-workflow.md