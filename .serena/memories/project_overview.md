# Unity MCP Test プロジェクト概要

## プロジェクトの目的
**Unity MCP Server** は、完全に実装されたModel Context Protocol (MCP) サーバーパッケージです。Unity Natural MCPを参考にしつつ、独自の改良を加えたUnity Editor統合MCPサーバーとして開発されました。

## 基本情報
- **パッケージ名**: com.umcp.unity-mcp-server
- **バージョン**: 1.0.0
- **Unityバージョン**: 2022.3 LTS以上
- **ライセンス**: MIT License
- **配布方式**: Unity Package (.unitypackage / Git URL)

## 主要機能
- **21個のビルトインツール**: 4カテゴリ（UnityInfo、AssetManagement、ConsoleLog、TestRunner、EditorExtension、ToolWorkflow）
- **Markdownベースワークフローシステム**: コンテキスト対応のインテリジェントワークフロー提案
- **HTTP + JSON-RPC 2.0**: localhost:49001/umcpでのMCPサーバー機能
- **属性ベース自動登録**: `[McpServerToolType]` + `[McpServerTool]`
- **依存性注入**: SimpleServiceContainerによる軽量DI

## アーキテクチャ
### コアシステム
- **UMcpServer**: HTTPサーバー + JSON-RPC 2.0プロトコル
- **UMcpSettings**: ProjectSettingsでの設定管理（ScriptableSingleton）
- **UMcpServerManager**: Unity Editor統合、自動起動、アセンブリリロード対応
- **UMcpToolBuilder**: ScriptableObjectベースのカスタムツール作成基底クラス

### 拡張フレームワーク
- 属性ベース自動登録システム
- SimpleServiceContainerによる軽量依存性注入
- Markdownワークフローパターン定義システム