# コードスタイルと規約

## EditorConfig設定
プロジェクトには`.editorconfig`ファイルが設定されており、以下の規約が適用されています：

### アクセス修飾子
- **private修飾子**: 暗黙的に省略可能
- **this修飾子**: インスタンスメンバーのthis修飾子は削除する

### 命名規則
- **privateフィールド**: camelCase（接頭辞なし）
- **publicプロパティ**: PascalCase
- **クラス名**: PascalCase
- **メソッド名**: PascalCase

### 波括弧規則
すべての制御構文で波括弧を必須とする：
- `if/else`文
- `for`/`foreach`ループ
- `while`/`do-while`ループ
- `using`文
- `lock`文
- `fixed`文

### コメント
- **日本語コメント**: XMLドキュメントコメントを含め日本語で記述
- **要約コメント**: `/// <summary>説明</summary>`形式を使用

## コーディングパターン

### 属性ベース実装
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

### 非同期処理パターン
```csharp
// 全Unity API呼び出し前に必須
await UniTask.SwitchToMainThread();
// CancellationTokenによるタイムアウト制御
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
```

## ファイル構成
- **1クラス1ファイル原則**: 1つのファイルには1つのpublicクラスのみ定義
- **内部クラス**: privateまたはファイル内でのみ使用される場合のみ許可
- **名前空間**: `uMCP.Editor`をルート名前空間として使用