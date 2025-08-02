#!/bin/bash

# MCPサーバー接続テストスクリプト

MCP_URL="http://localhost:49001/umcp"

echo "=== MCPサーバー接続テスト ==="
echo "URL: $MCP_URL"
echo ""

# 1. 単純なGETリクエスト
echo "1. GETリクエストテスト:"
curl -s "$MCP_URL" || echo "GETリクエスト失敗"
echo -e "\n"

# 2. POSTリクエスト（ツール一覧取得）
echo "2. ツール一覧取得:"
curl -s -X POST "$MCP_URL" \
    -H "Content-Type: application/json" \
    -d '{
        "jsonrpc": "2.0",
        "method": "tools/list",
        "params": {},
        "id": 1
    }' | head -100
echo -e "\n"

# 3. 並列処理版インデックス再構築（生のレスポンス）
echo "3. 並列処理版インデックス再構築（生のレスポンス）:"
curl -X POST "$MCP_URL" \
    -H "Content-Type: application/json" \
    -d '{
        "jsonrpc": "2.0",
        "method": "tools/call",
        "params": {
            "name": "rebuild_documentation_index_parallel",
            "arguments": {}
        },
        "id": 1
    }' -v 2>&1 | grep -E "(< HTTP|< |{|})"