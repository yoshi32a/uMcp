# コードベース構造

## プロジェクトルート構造
```
/mnt/c/Users/myunp/workspace/UnityMcpTest/
├── Assets/                          # Unity アセットルート
│   ├── uMcp/                       # MCPサーバーパッケージ
│   ├── Scenes/                     # Unityシーン  
│   ├── Editor/                     # エディタースクリプト
│   └── [その他Unityアセット]
├── ProjectSettings/                 # Unity プロジェクト設定
├── Packages/                       # Package Manager管理
├── CLAUDE.md                       # 開発指針ドキュメント
├── README.md                       # プロジェクト説明
└── .editorconfig                   # コードスタイル設定
```

## uMCPパッケージ構造
**パッケージルート**: `Assets/uMcp/`

```
Assets/uMcp/
├── package.json                    # パッケージ定義
├── README.md                      # メインドキュメント  
├── CHANGELOG.md                   # 変更履歴
├── LICENSE.md                     # MITライセンス
│
├── Editor/                        # エディター専用コード
│   ├── uMCP.Editor.asmdef        # エディターアセンブリ定義
│   │
│   ├── Core/                     # MCPサーバーコア実装
│   │   ├── UMcpServer.cs        # HTTPサーバー
│   │   ├── UMcpServerManager.cs # Unity統合・ライフサイクル  
│   │   ├── UMcpToolBuilder.cs   # ツール基底クラス
│   │   ├── McpSession.cs        # セッション管理
│   │   ├── SessionManager.cs    # セッションマネージャー
│   │   │
│   │   ├── Attributes/          # カスタム属性
│   │   │   ├── DescriptionAttribute.cs
│   │   │   ├── McpServerToolAttribute.cs
│   │   │   └── McpServerToolTypeAttribute.cs
│   │   │
│   │   ├── DependencyInjection/ # 軽量DI実装
│   │   │   ├── SimpleServiceContainer.cs
│   │   │   └── ServiceCollectionBuilder.cs
│   │   │
│   │   └── Protocol/            # JSON-RPC 2.0プロトコル
│   │       ├── JsonRpcRequest.cs
│   │       ├── JsonRpcResponse.cs
│   │       ├── CallToolRequest.cs
│   │       ├── CallToolResult.cs
│   │       └── [その他プロトコル定義]
│   │
│   ├── Settings/                # 設定管理
│   │   └── UMcpSettings.cs     # ProjectSettings統合
│   │
│   └── Tools/                   # ビルトインツール実装（21ツール）
│       ├── UnityInfo/           # Unity情報ツール（5ツール）
│       ├── AssetManagement/     # アセット管理ツール（5ツール）
│       ├── ConsoleLog/          # コンソールログツール（4ツール）
│       ├── TestRunner/          # テスト実行ツール（3ツール）
│       ├── EditorExtension/     # エディタ拡張ツール（1ツール）
│       ├── ToolWorkflow/        # ワークフロー提案ツール（2ツール）
│       └── ErrorResponse.cs     # 共通エラーレスポンス
│
├── Workflows/                   # Markdownワークフロー定義
│   ├── editor-extension-workflow.md
│   ├── error-investigation-workflow.md
│   ├── test-execution-workflow.md
│   ├── asset-management-workflow.md
│   └── workflow-triggers.md
│
├── Tests/                       # ユニットテスト
│   ├── uMcp.Tests.asmdef       # テストアセンブリ定義
│   └── PlayModeTests.cs        # PlayModeテスト実装
│
└── Samples~/                    # サンプルコード（オプション）
    └── BasicTools/
        ├── CustomToolExample.cs
        └── README.md
```

## 重要な設定ファイル
- **`/ProjectSettings/uMcpSettings.asset`**: MCPサーバー設定（自動生成）
- **`Assets/uMcp/Editor/uMCP.Editor.asmdef`**: エディターアセンブリ定義
- **`Assets/uMcp/package.json`**: パッケージメタデータ