#!/bin/bash

# インデックス再構築の進捗モニタリングスクリプト

# 色付き出力
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${YELLOW}=== Unity MCP インデックス再構築モニター ===${NC}"
echo "Unity Editorのコンソールログを確認してください"
echo ""

# Unityログファイルの場所（Windows）
UNITY_LOG="/mnt/c/Users/myunp/AppData/Local/Unity/Editor/Editor.log"

if [ -f "$UNITY_LOG" ]; then
    echo -e "${CYAN}Unity Editor ログをモニタリング中...${NC}"
    echo -e "${GREEN}Ctrl+C で終了${NC}"
    echo ""
    
    # 最新のログをリアルタイムで表示
    tail -f "$UNITY_LOG" | grep -E "(並列処理|インデックス|処理中|完了|エラー|ファイル|Manual|ScriptReference|メモリ|処理完了)"
else
    echo -e "${YELLOW}Unity Editor ログファイルが見つかりません${NC}"
    echo "Unity Editorコンソールで直接確認してください："
    echo ""
    echo "期待されるログ出力："
    echo "- 🚀 並列処理開始: 最大8並列"
    echo "- 📂 Manualディレクトリを処理中"
    echo "- 📄 Manual: 3205ファイルを並列処理"
    echo "- ✅ Manual処理完了: 3205ファイル"
    echo "- 🎉 並列インデックス構築完了: XXXXエントリ, XXXXキーワード (XXXms)"
fi

# 別ウィンドウでステータス確認コマンドも表示
echo ""
echo -e "${YELLOW}=== 別のターミナルで実行可能なコマンド ===${NC}"
echo ""
echo "# インデックスファイルのサイズ確認:"
echo "ls -lh /mnt/c/Users/myunp/AppData/LocalLow/Unity/*/uMcp_*.json 2>/dev/null"
echo ""
echo "# 詳細エントリファイル数確認:"
echo "find /mnt/c/Users/myunp/AppData/LocalLow/Unity/*/uMcp_ParallelDetailIndex -name '*.json' 2>/dev/null | wc -l"
echo ""
echo "# プロセス確認:"
echo "ps aux | grep -i unity"