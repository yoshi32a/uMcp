# Unity MCP Server v{VERSION}

## 🎉 主な変更点

<!-- ここにリリースの主要な変更点を記載 -->

## ✨ 新機能

<!-- 新しく追加された機能 -->

## 🐛 バグ修正

<!-- 修正されたバグ -->

## 📝 ドキュメント

<!-- ドキュメントの更新 -->

## 🔧 改善

<!-- パフォーマンスや使い勝手の改善 -->

## ⚠️ 破壊的変更

<!-- 互換性のない変更がある場合 -->

## 📦 インストール方法

### 方法1: 手動インストール
1. `com.umcp.unity-mcp-server-{VERSION}.tgz` をダウンロード
2. Unity プロジェクトの `Assets` フォルダに展開

### 方法2: Unity Package Manager (ローカル)
1. `com.umcp.unity-mcp-server-{VERSION}-upm.tgz` をダウンロード  
2. Unity プロジェクトの `Packages` フォルダに展開

### 方法3: Unity Package Manager (tarball)
1. `com.umcp.unity-mcp-server-{VERSION}-upm.tgz` をダウンロード
2. Unity で Package Manager を開く
3. 「+」→「Add package from tarball...」をクリック
4. ダウンロードした .tgz ファイルを選択

### 方法4: Git URL
`manifest.json` に以下を追加:
```json
"com.umcp.unity-mcp-server": "https://github.com/{REPOSITORY}.git#{TAG}"
```

## 📋 必要環境
- Unity 2022.3 LTS 以上
- UniTask 2.3.3 以上

## 🚀 使い方
1. パッケージをインストール
2. Unity Editor を開く
3. `Tools > uMCP > Create Default Tool Assets` を実行
4. MCP サーバーが `http://localhost:49001/umcp/` で自動起動

詳細は [README](https://github.com/{REPOSITORY}/blob/{TAG}/Assets/uMcp/README.md) を参照してください。

## 🤝 コントリビューター

<!-- コントリビューターへの感謝 -->

---

**Full Changelog**: https://github.com/{REPOSITORY}/compare/{PREVIOUS_TAG}...{TAG}