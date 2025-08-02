# Unity Documentation Search Workflow

## 概要
Unity公式ドキュメントの並列高速検索とインデックス管理のワークフロー

## ワークフローパターン

### 1. 初回セットアップ（並列インデックス構築）

**トリガー:**
- 新しいUnityバージョン使用時
- 初回ドキュメント検索時
- インデックスファイル不存在時

**実行手順:**
```
1. rebuild_documentation_index
   - 並列処理でドキュメントインデックスを構築
   - Manual/ScriptReferenceを最大8並列で処理
   - 完了まで2-3分（初回のみ）

2. search_documentation (テスト実行)
   - query: "Vector3"
   - searchType: "ScriptReference"
   - maxResults: 5
   - インデックス構築完了を確認

3. get_unity_info
   - Unityバージョンとプロジェクト情報を確認
   - インデックス有効性を検証
```

### 2. 高速ドキュメント検索

**トリガー:**
- Unity API調査時
- 機能実装前の公式情報確認
- エラー解決のための公式ドキュメント参照

**実行手順:**
```
1. search_documentation
   - query: "{検索キーワード}"
   - searchType: "All" | "Manual" | "ScriptReference"
   - maxResults: 10
   - 1秒以内で結果取得

2. log_to_console (オプション)
   - 検索結果をUnityコンソールに記録
   - 開発者間での情報共有用

3. save_project (推奨)
   - 検索結果に基づく実装前の保存
```

### 3. インデックス更新・メンテナンス

**トリガー:**
- Unityバージョンアップグレード後
- インデックス構築から7日経過時
- 検索結果が不正確な場合

**実行手順:**
```
1. get_unity_info
   - 現在のUnityバージョンを確認
   - プロジェクト状態を把握

2. rebuild_documentation_index
   - 既存インデックスをクリア
   - 並列処理で新しいインデックスを構築
   - 構築時間とエントリ数を確認

3. search_documentation (検証)
   - query: "Transform"
   - 検索精度と速度を確認
   - インデックス更新成功を検証

4. get_log_statistics
   - Unity コンソールでの処理状況確認
   - エラーや警告がないことを確認
```

### 4. パフォーマンス最適化検証

**トリガー:**
- 検索速度低下時
- メモリ使用量増加時
- インデックスサイズ肥大化時

**実行手順:**
```
1. get_console_logs
   - errorsOnly: false
   - maxLogs: 20
   - 検索関連のログを確認

2. search_documentation (ベンチマーク)
   - 複数の検索クエリで速度測定
   - メモリ使用量をモニタリング

3. rebuild_documentation_index (必要時)
   - インデックス最適化
   - パフォーマンス改善確認

4. save_project
   - 最適化後の状態を保存
```

## 推奨パラメータ

### search_documentation
- **一般的なAPI検索**: searchType: "ScriptReference", maxResults: 10
- **機能理解**: searchType: "Manual", maxResults: 5
- **包括的調査**: searchType: "All", maxResults: 15

### rebuild_documentation_index
- **パラメータ**: なし（並列処理自動実行）
- **実行頻度**: 新Unityバージョン使用時、週1回程度
- **実行タイミング**: 開発開始前、長時間作業の前

## パフォーマンス指標

### 期待値
- **インデックス構築**: 2-3分（初回）、1-2分（更新）
- **検索実行**: 1秒以内
- **メモリ使用量**: 50-100MB（インデックスキャッシュ）
- **エントリ数**: 6000+（Manual + ScriptReference）
- **キーワード数**: 145,000+

### 最適化のサイン
- 検索時間 > 2秒 → インデックス再構築検討
- メモリ使用量 > 200MB → インデックス最適化必要
- エラー発生 → Unity バージョン不整合確認

## トラブルシューティング

### よくある問題
1. **インデックス構築失敗**
   - Unity ドキュメントパス確認
   - 管理者権限で実行
   - ディスク容量確認

2. **検索結果が空**
   - インデックス有効性確認
   - Unity バージョン整合性確認
   - クエリ文字列確認

3. **パフォーマンス低下**
   - インデックスキャッシュクリア
   - Unity エディタ再起動
   - システムリソース確認

### デバッグ手順
```
1. get_unity_info → システム状態確認
2. get_console_logs → エラーログ確認
3. rebuild_documentation_index → インデックス再構築
4. search_documentation → 機能確認
```