# 開発ガイドライン

## 基本方針
このプロジェクトは**完成したUnityパッケージ**です。以下の原則を厳守してください：

### 変更方針
- **必要最小限の変更のみ**: 要求されていない機能追加は行わない
- **既存ファイル優先**: 新規ファイル作成より既存ファイルの編集を優先  
- **ドキュメント作成制限**: 明示的に要求されない限りドキュメントファイル(.md)は作成しない

## パフォーマンス考慮事項

### 必須実装パターン
#### 非同期処理とスレッド管理
```csharp
// 全Unity API呼び出し前に必須
await UniTask.SwitchToMainThread();
// CancellationTokenによるタイムアウト制御
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
```

#### PlayModeテスト設定保存・復元パターン
```csharp
// メソッドレベルで変数宣言
bool originalEnterPlayModeOptionsEnabled = false;
EnterPlayModeOptions originalEnterPlayModeOptions = EnterPlayModeOptions.None;

// 設定変更
if (disableDomainReload) {
    originalEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
    originalEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
    EditorSettings.enterPlayModeOptionsEnabled = true;
    EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
}

// finally句での確実な復元
finally {
    if (testMode == TestMode.PlayMode && disableDomainReload) {
        EditorSettings.enterPlayModeOptionsEnabled = originalEnterPlayModeOptionsEnabled;
        EditorSettings.enterPlayModeOptions = originalEnterPlayModeOptions;
    }
}
```

## カスタムツール開発パターン

### ツール実装手順
1. `UMcpToolBuilder`継承でScriptableObject作成
2. `[McpServerToolType]`でツールクラス定義
3. `[McpServerTool]`で個別メソッド定義
4. パラメータには`[Description]`属性を付与

### 実装例
```csharp
[McpServerToolType, Description("ツールの説明")]
internal sealed class MyToolImplementation
{
    [McpServerTool, Description("メソッドの説明")]
    public async ValueTask<object> MyMethod(
        [Description("パラメータ説明")] string param = "デフォルト値")
    {
        await UniTask.SwitchToMainThread();
        return new MyResponse { Success = true };
    }
}
```

## パッケージ整合性維持

### 保護すべき設定
- **package.json**: バージョン、依存関係、メタデータの整合性を保つ
- **uMCP.Editor.asmdef**: アセンブリ定義設定を変更しない
- **ディレクトリ構造**: 確立されたパッケージ構造を維持

### コード品質基準
- **既存パターン準拠**: UMcpToolBuilder、属性ベース登録の既存パターンに従う
- **Unity API適切使用**: UniTask.SwitchToMainThread()の適切な使用
- **エラーハンドリング**: 既存の堅牢なエラー処理パターンを維持
- **1クラス1ファイル原則**: 厳密に遵守

## ドキュメント更新指針

### CLAUDE.md更新方針
- **技術的実装詳細に集中**: 実装パターンとコード例を記載
- **開発指針を優先**: 開発者が従うべき技術的ガイドラインを重視
- **実装パターンの文書化**: 成功した技術的解決策は必須実装パターンとして記録
- **コード例の提供**: 重要な実装については具体的なコード例を含める

### 役割分担
- **CLAUDE.md**: 開発者向け技術指針・実装パターン・アーキテクチャ詳細
- **README.md**: ユーザー向け機能説明・使用方法・トラブルシューティング