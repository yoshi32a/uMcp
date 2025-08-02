#!/bin/bash

# Unity MCP Documentation Index Rebuild Script
# 使い方: ./rebuild_index.sh [parallel|sequential|search]

MCP_URL="http://localhost:49001/umcp"

# 色付き出力
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 時間計測関数
time_command() {
    local start_time=$(date +%s)
    "$@"
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))
    echo -e "${GREEN}実行時間: $((duration / 60))分 $((duration % 60))秒${NC}"
}

# 並列処理版インデックス再構築
rebuild_parallel() {
    echo -e "${YELLOW}=== 並列処理版インデックス再構築開始 ===${NC}"
    time_command curl -X POST "$MCP_URL" \
        -H "Content-Type: application/json" \
        -d '{
            "jsonrpc": "2.0",
            "method": "tools/call",
            "params": {
                "name": "rebuild_documentation_index_parallel",
                "arguments": {}
            },
            "id": 1
        }' -v
}

# 逐次処理版インデックス再構築
rebuild_sequential() {
    echo -e "${YELLOW}=== 逐次処理版インデックス再構築開始 ===${NC}"
    time_command curl -X POST "$MCP_URL" \
        -H "Content-Type: application/json" \
        -d '{
            "jsonrpc": "2.0",
            "method": "tools/call",
            "params": {
                "name": "rebuild_documentation_index",
                "arguments": {}
            },
            "id": 1
        }' -v
}

# ドキュメント検索テスト
search_test() {
    local query="${2:-Vector3}"
    echo -e "${YELLOW}=== ドキュメント検索: '$query' ===${NC}"
    time_command curl -X POST "$MCP_URL" \
        -H "Content-Type: application/json" \
        -d "{
            \"jsonrpc\": \"2.0\",
            \"method\": \"tools/call\",
            \"params\": {
                \"name\": \"search_documentation\",
                \"arguments\": {
                    \"query\": \"$query\",
                    \"searchType\": \"All\",
                    \"maxResults\": 5
                }
            },
            \"id\": 1
        }" | jq '.'
}

# メインロジック
case "$1" in
    parallel|p)
        rebuild_parallel
        ;;
    sequential|s)
        rebuild_sequential
        ;;
    search)
        search_test "$@"
        ;;
    *)
        echo "Unity MCP Documentation Index Manager"
        echo ""
        echo "使用方法:"
        echo "  ./rebuild_index.sh parallel    # 並列処理版でインデックス再構築"
        echo "  ./rebuild_index.sh sequential  # 逐次処理版でインデックス再構築"
        echo "  ./rebuild_index.sh search [query]  # ドキュメント検索（デフォルト: Vector3）"
        echo ""
        echo "短縮形:"
        echo "  ./rebuild_index.sh p          # parallel"
        echo "  ./rebuild_index.sh s          # sequential"
        echo ""
        echo "例:"
        echo "  ./rebuild_index.sh p"
        echo "  ./rebuild_index.sh search NavMesh"
        exit 1
        ;;
esac