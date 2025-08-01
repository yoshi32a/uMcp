# 推奨コマンド・開発ワークフロー

## システムコマンド（Linux環境）

### ファイル操作
- `ls -la`: ファイル一覧表示（詳細表示）
- `find . -name "*.cs"`: C#ファイル検索
- `grep -r "pattern" .`: テキスト検索
- `cd path/to/directory`: ディレクトリ移動

### Git操作 
- `git status`: 変更状況確認
- `git add .`: 全変更をステージング
- `git commit -m "message"`: コミット
- `git push`: リモートプッシュ
- `git pull`: リモートから更新取得

## Unity開発コマンド

### Unity Editor操作
Unityプロジェクトの開発は主にUnity Editor GUI内で行われます：

#### MCPサーバー管理
- **Tools > uMCP > Start Server**: MCPサーバー手動起動
- **Tools > uMCP > Stop Server**: MCPサーバー停止
- **Tools > uMCP > Show Server Info**: サーバー状態確認
- **Tools > uMCP > Create Default Tool Assets**: デフォルトツールアセット作成

#### テスト実行
- **Window > General > Test Runner**: Unity Test Runnerウィンドウ表示
- **EditMode Tests**: エディターモードテスト実行
- **PlayMode Tests**: プレイモードテスト実行（ドメインリロード最適化対応）

### MCP機能テスト
MCPサーバーが起動している場合：
- **URL**: `http://localhost:49001/umcp/`でMCPクライアント接続
- **21個のビルトインツール**が利用可能
- **ワークフロー提案機能**でコンテキスト対応の次アクション提案

## パッケージ開発ワークフロー

### 開発サイクル
1. **コード編集**: Assets/uMcp/内のC#ファイル編集
2. **Unity再コンパイル**: 自動実行（アセンブリリロード）
3. **MCPサーバー自動再起動**: UMcpServerManagerが自動処理
4. **機能テスト**: MCP接続でツール動作確認
5. **Unityテスト実行**: Test Runnerでユニットテスト
6. **Git コミット**: 変更をバージョン管理

### デバッグ手順
1. **デバッグモード有効化**: UMcpSettingsでリクエスト/レスポンス詳細ログ
2. **コンソールログ確認**: Unity Consoleでエラー・警告確認
3. **サーバー状態確認**: Tools > uMCP > Show Server Info
4. **ポート競合チェック**: デフォルト49001番ポートの使用状況確認

## タスク完了時の推奨アクション
1. **Unity テスト実行**: EditMode/PlayModeテストで動作確認
2. **MCPサーバー動作確認**: 21ツールの動作テスト
3. **コード品質チェック**: .editorconfigに従ったスタイル確認
4. **ドキュメント更新**: 必要に応じてCLAUDE.md更新
5. **Git コミット**: 変更の適切なコミット