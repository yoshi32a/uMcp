# Junie ガイドライン（プロジェクト概要付き）

このドキュメントは、JetBrains 製 AI アシスタント「Junie」が本リポジトリで作業する際の指針と、プロジェクトの概要を示します。最小変更を基本方針とし、必要十分な対応に留めてください。

## プロジェクト概要
- 名称: Unity MCP Server（uMcp）
- 目的: Unity Editor と AI アシスタント/クライアントを Model Context Protocol (MCP) で接続し、エディタ自動化・ドキュメント検索・テスト実行などを可能にする。
- 主な機能:
  - HTTP ベース MCP サーバー（デフォルト: http://localhost:49001/umcp/）
  - JSON-RPC 2.0 準拠、Unity 起動時の自動サーバー開始
  - エディタ統合ツール群（Unity 情報取得、アセット管理、コンソールログ、テストランナー、ワークフロー提案 など）
- 詳細: ルートの README_ja.md を参照

## 前提・開発環境
- Unity: 6000.1.10f1（README では 2022.3 LTS 以降推奨）
- 依存: UniTask など（README_ja.md の手順に従って導入）
- 推奨 IDE: JetBrains Rider / Visual Studio（Windows パス区切り: バックスラッシュ）

## ディレクトリ構成（高レベル）
- Assets\uMcp\...: MCP サーバーおよび各種 Editor ツール
  - Assets\uMcp\Editor\Tools\DocumentationSearch\...: ドキュメント検索ツール実装
  - Assets\uMcp\Workflows\...: ワークフロー関連ドキュメント
- Packages, ProjectSettings: Unity 標準構成
- 各種 .csproj / .sln: エディタ/テスト/拡張用 C# プロジェクト
- README_ja.md: 詳細な機能説明とセットアップ手順
- .junie\guidelines.md: 本ガイドライン

## ビルド・実行
- Unity エディタでプロジェクトを開くと、MCP サーバーは既定で自動起動
- メニュー: Tools > uMCP からサーバー制御やツールアセット作成
- MCP クライアント例: MCP Inspector（HTTP, http://localhost:49001/umcp/）

## テスト方針
- Unity Test Runner（EditMode / PlayMode）での実行を想定
- 追加の C# テストプロジェクト（例: uMcp.Tests.csproj）が存在する場合は IDE から実行可能
- 失敗テストの再現・最小修正・回帰防止を重視

## コードスタイル（簡易）
- C#: Microsoft/Unity 標準に準拠（PascalCase/camelCase、早期 return、null 安全、例外より Guard）
- Unity: ScriptableObject/Editor 拡張の責務分離、アセット再インポートやドメインリロードの副作用に注意
- ログ: Editor 拡張は必要な箇所に限定してログ出力（デバッグフラグ活用）

## Junie 作業ガイド
- 最小変更の原則: 目的達成に必要な差分に限定
- ツール使用: 専用ツール（search_project, search_replace, run_test 等）を優先し、Windows 環境の制約に合わせる
- パス表記: すべてバックスラッシュ（例: Assets\uMcp\...）
- ドキュメント更新のみの場合はビルド/テスト不要。コード変更時は関連テストの実行を推奨
- 重大な不明点がある場合は User に確認する

## 変更/PR 方針（簡易）
- 目的・根拠が明確な小さな PR を推奨
- 既存の挙動・API 互換性を壊さない（破壊的変更は要告知）
- ドキュメント（README_ja.md 等）との整合を保つ

## 参考
- 詳細手順・機能一覧: README_ja.md
- 代表的なソース: Assets\uMcp\Editor\Tools\...
