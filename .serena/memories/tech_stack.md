# 技術スタック

## プログラミング言語
- **C#**: Unity Editor拡張として実装

## 主要フレームワーク・ライブラリ
- **Unity 2022.3 LTS以上**: ベースプラットフォーム
- **UniTask (2.3.3+)**: 非同期処理・メインスレッド同期
- **System.Text.Json (9.0.7+)**: 独自MCPプロトコル実装
- **Unity Test Framework**: テスト実行機能

## アセンブリ定義
- **uMCP.Editor**: メインエディターアセンブリ（UniTask, UniTask.Editor参照）
- **uMcp.Tests**: テスト専用アセンブリ

## 通信プロトコル
- **HTTP/1.1サーバー**: 堅牢なHTTPリクエスト処理
- **JSON-RPC 2.0**: 完全なMCPプロトコル実装
- **CORS対応**: Web基盤MCPクライアント対応

## 開発ツール
- **.editorconfig**: コードスタイル統一
- **Unity Package Manager**: パッケージ管理
- **Git**: バージョン管理

## アーキテクチャパターン
- **依存性注入**: SimpleServiceContainer
- **属性ベースリフレクション**: 自動ツール登録
- **ScriptableObject**: 設定とツール管理
- **非同期プログラミング**: UniTaskベース