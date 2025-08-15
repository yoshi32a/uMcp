# Unity MCP Server

UnityエディターでAIアシスタントがUnityプロジェクトとリアルタイムでやり取りできる強力なModel Context Protocol (MCP) サーバー実装です。インテリジェントなワークフローガイダンス機能を搭載しています。

## 概要

Unity MCP Server は Unity Editor 内で動作する完全なMCPサーバー実装を提供し、ClaudeなどのAIアシスタントがUnityプロジェクトと直接やり取りできるようにします。アセット管理、コンソールログ、テスト実行、プロジェクト分析、コンテキスト対応開発ワークフロー用の**25個のビルトインツール**、**Markdownベースワークフローシステム**、**インテリジェントなアクション提案**を特徴とします。

## ドキュメント

- [完全ドキュメント](Assets/uMcp/README.md)

## クイックスタート

### 前提条件

1. **Unity 2022.3 LTS** 以降
2. **UniTask** - Package Manager経由でインストール: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`

### インストール

```
https://github.com/yoshi32a/uMcp.git?path=Assets/uMcp
```

上記URLをUnity Package Managerに追加してください。

## 機能

- 🚀 **HTTPサーバー** `localhost:49001/umcp/` で動作
- 🛠️ **25個のビルトインツール** 6カテゴリでUnityとの連携
- 🧠 **Markdownワークフローシステム** アクション提案付き
- ⚡ **PlayModeテスト** ドメインリロード制御機能
- 🔍 **エラー検出** 修正されたコンソールログフィルタリング
- 📦 **簡単統合** 自動起動機能付き
- 🔧 **拡張可能フレームワーク** カスタムツール対応

### 最新版の新機能
- **🆕 ワークフローガイダンス**: 次アクション提案
- **📝 Markdownワークフロー**: 編集しやすいワークフロー定義
- **🎯 自動トリガー**: 自動ツール連携
- **🐛 バグ修正**: `get_console_logs` のerrorsOnlyフィルタリング問題を解決

## ライセンス

MIT License - 詳細は [LICENSE.md](LICENSE.md) をご覧ください。

## ツールカテゴリ

### 🎯 Unity情報 (5ツール)
- プロジェクト分析、シーン検査、GameObjectの詳細

### 📁 アセット管理 (5ツール)  
- アセット検索、リフレッシュ、インポート管理

### 🐛 コンソールログ (4ツール)
- ログ取得、フィルタリング、統計（errorsOnlyバグ修正済み）

### 🧪 テスト実行 (3ツール)
- ドメインリロード最適化付きEditMode/PlayModeテスト実行

### ⚙️ エディター拡張 (1ツール)
- 開発自動化用カスタムメソッド実行

### 🧠 ワークフローガイダンス (2ツール) **新機能！**
- インテリジェントな次アクション提案とMarkdownワークフローパターン

## リポジトリ構造

```
UnityMcpTest/
├── Assets/
│   ├── uMcp/              # Unity MCP Serverパッケージ
│   │   ├── Editor/        # コア実装
│   │   ├── Workflows/     # Markdownワークフロー定義
│   │   ├── package.json   # パッケージマニフェスト
│   │   └── README.md      # 完全ドキュメント
│   └── packages.config    # NuGetパッケージ
├── README.md              # このファイル
└── CLAUDE.md              # AIアシスタント指示書
```
