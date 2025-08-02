 curl -X POST http://localhost:49001/umcp \
    -H "Content-Type: application/json" \
    -d '{                                                                                                                                                                                                                    
      "jsonrpc": "2.0",                                                                                                                                                                                                      
      "method": "tools/call",                                                                                                                                                                                                
      "params": {                                                                                                                                                                                                            
        "name": "rebuild_documentation_index_parallel",                                                                                                                                                                      
        "arguments": {}                                                                                                                                                                                                      
      },                                                                                                                                                                                                                     
      "id": 1                                                                                                                                                                                                                
    }'