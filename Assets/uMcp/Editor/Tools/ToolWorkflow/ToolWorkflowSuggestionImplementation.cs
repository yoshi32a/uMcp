using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using uMCP.Editor.Core.Attributes;

namespace uMCP.Editor.Tools
{
    /// <summary>MCPツールの連携ワークフロー提案システム</summary>
    [McpServerToolType, Description("MCPツール連携とワークフロー提案")]
    internal sealed class ToolWorkflowSuggestionImplementation
    {
        /// <summary>現在のコンテキストから次の推奨アクションを提案</summary>
        [McpServerTool, Description("現在の状態から推奨される次のMCPツール実行を読みやすい形式で提案")]
        public async ValueTask<object> GetNextActionSuggestions(
            [Description("直前に実行したツール名")] string lastExecutedTool = "",
            [Description("現在の作業コンテキスト")] string workContext = "")
        {
            await UniTask.SwitchToMainThread();

            var suggestions = new List<WorkflowSuggestion>();

            // Markdownからトリガー定義を読み込み
            var triggers = WorkflowMarkdownParser.ParseTriggerFile();

            // ツールベースの提案
            if (!string.IsNullOrEmpty(lastExecutedTool) && 
                triggers.ToolBasedTriggers.ContainsKey(lastExecutedTool))
            {
                foreach (var trigger in triggers.ToolBasedTriggers[lastExecutedTool])
                {
                    suggestions.Add(new WorkflowSuggestion
                    {
                        Tool = trigger.Tool,
                        Reason = $"{lastExecutedTool}実行後の推奨アクション",
                        Priority = trigger.Priority.ToLower(),
                        Parameters = trigger.Parameters
                    });
                }
            }

            // コンテキストベースの提案
            if (!string.IsNullOrEmpty(workContext))
            {
                var workflows = WorkflowMarkdownParser.LoadAllWorkflows();
                
                foreach (var workflow in workflows)
                {
                    // タグやトリガー条件でマッチング
                    bool matches = workflow.Tags.Any(tag => 
                        workContext.Contains(tag, StringComparison.OrdinalIgnoreCase));
                    
                    if (matches && workflow.Steps.Count > 0)
                    {
                        // ワークフローの最初のステップを提案
                        var firstStep = workflow.Steps[0];
                        suggestions.Add(new WorkflowSuggestion
                        {
                            Tool = firstStep.ToolName,
                            Reason = $"{workflow.Name}の開始",
                            Priority = "high",
                            Parameters = firstStep.Parameters,
                            WorkflowName = workflow.Name
                        });
                    }
                }
            }

            // 重複を削除
            suggestions = suggestions
                .GroupBy(s => s.Tool)
                .Select(g => g.First())
                .ToList();

            // 一般的な推奨事項を追加
            AddGeneralSuggestions(suggestions, lastExecutedTool);

            // 優先度順にソート
            var sortedSuggestions = suggestions.OrderByDescending(s => GetPriorityValue(s.Priority)).ToList();

            // 読みやすい形式のサマリーを作成
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("=== 推奨アクション一覧 ===");
            summary.AppendLine($"**前回実行ツール:** {(string.IsNullOrEmpty(lastExecutedTool) ? "なし" : lastExecutedTool)}");
            summary.AppendLine($"**作業コンテキスト:** {(string.IsNullOrEmpty(workContext) ? "一般的な作業" : workContext)}");
            summary.AppendLine();

            if (sortedSuggestions.Count > 0)
            {
                // 優先度別グループ化
                var highPriority = sortedSuggestions.Where(s => s.Priority == "high").ToList();
                var mediumPriority = sortedSuggestions.Where(s => s.Priority == "medium").ToList(); 
                var lowPriority = sortedSuggestions.Where(s => s.Priority == "low").ToList();

                if (highPriority.Count > 0)
                {
                    summary.AppendLine("## 🔥 高優先度の推奨アクション");
                    foreach (var suggestion in highPriority)
                    {
                        summary.AppendLine($"- **{suggestion.Tool}** - {suggestion.Reason}");
                        if (!string.IsNullOrEmpty(suggestion.WorkflowName))
                        {
                            summary.AppendLine($"  関連ワークフロー: {suggestion.WorkflowName}");
                        }
                    }
                    summary.AppendLine();
                }

                if (mediumPriority.Count > 0)
                {
                    summary.AppendLine("## ⚡ 中優先度の推奨アクション");
                    foreach (var suggestion in mediumPriority)
                    {
                        summary.AppendLine($"- **{suggestion.Tool}** - {suggestion.Reason}");
                        if (!string.IsNullOrEmpty(suggestion.WorkflowName))
                        {
                            summary.AppendLine($"  関連ワークフロー: {suggestion.WorkflowName}");
                        }
                    }
                    summary.AppendLine();
                }

                if (lowPriority.Count > 0)
                {
                    summary.AppendLine("## 💡 低優先度の推奨アクション");
                    foreach (var suggestion in lowPriority)
                    {
                        summary.AppendLine($"- **{suggestion.Tool}** - {suggestion.Reason}");
                        if (!string.IsNullOrEmpty(suggestion.WorkflowName))
                        {
                            summary.AppendLine($"  関連ワークフロー: {suggestion.WorkflowName}");
                        }
                    }
                    summary.AppendLine();
                }

                // 次のステップガイダンス
                summary.AppendLine("## 📋 次のステップ");
                var topSuggestion = sortedSuggestions.First();
                summary.AppendLine($"**最初に実行すべき:** `{topSuggestion.Tool}`");
                summary.AppendLine($"**理由:** {topSuggestion.Reason}");
                if (topSuggestion.Parameters != null && HasParameters(topSuggestion.Parameters))
                {
                    summary.AppendLine($"**推奨パラメータ:** {System.Text.Json.JsonSerializer.Serialize(topSuggestion.Parameters, new System.Text.Json.JsonSerializerOptions { WriteIndented = false, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping })}");
                }
            }
            else
            {
                summary.AppendLine("**推奨アクションはありません。**");
                summary.AppendLine("現在の状況では特定の次ステップは提案されていません。");
                summary.AppendLine("一般的な作業として `get_unity_info` や `get_scene_info` から始めることをお勧めします。");
            }

            return new StandardResponse
            {
                Success = true,
                FormattedOutput = summary.ToString()
            };
        }

        /// <summary>一般的な推奨事項を追加</summary>
        void AddGeneralSuggestions(List<WorkflowSuggestion> suggestions, string lastTool)
        {
            // 重複を避ける
            var existingTools = suggestions.Select(s => s.Tool).ToHashSet();

            if (!existingTools.Contains("save_project"))
            {
                suggestions.Add(new WorkflowSuggestion
                {
                    Tool = "save_project",
                    Reason = "作業内容を保存",
                    Priority = "low"
                });
            }

            if (!existingTools.Contains("get_scene_info") && lastTool != "get_scene_info")
            {
                suggestions.Add(new WorkflowSuggestion
                {
                    Tool = "get_scene_info",
                    Reason = "現在のシーン状態を確認",
                    Priority = "low"
                });
            }
        }

        /// <summary>パラメータが存在するかチェック</summary>
        bool HasParameters(object parameters)
        {
            if (parameters == null) return false;
            
            if (parameters is System.Collections.IDictionary dict)
                return dict.Count > 0;
            
            if (parameters is System.Collections.ICollection collection)
                return collection.Count > 0;
                
            return !string.IsNullOrEmpty(parameters.ToString());
        }

        /// <summary>優先度を数値に変換</summary>
        int GetPriorityValue(string priority)
        {
            return priority?.ToLower() switch
            {
                "high" => 3,
                "medium" => 2,
                "low" => 1,
                _ => 0
            };
        }

        /// <summary>特定のツールシーケンスのワークフロー情報を取得</summary>
        [McpServerTool, Description("Markdownファイルから読み込んだワークフローパターンを読みやすい形式で取得")]
        public async ValueTask<object> GetWorkflowPatterns()
        {
            await UniTask.SwitchToMainThread();

            var workflows = WorkflowMarkdownParser.LoadAllWorkflows();
            var result = new System.Text.StringBuilder();
            
            result.AppendLine("=== ワークフローパターン一覧 ===");
            result.AppendLine();

            // 空でないワークフローのみを表示
            var validWorkflows = workflows.Where(w => !string.IsNullOrEmpty(w.Name) && 
                                                     (!string.IsNullOrEmpty(w.Description) || w.Steps.Count > 0))
                                         .ToList();

            foreach (var workflow in validWorkflows)
            {
                result.AppendLine($"## {workflow.Name}");
                if (!string.IsNullOrEmpty(workflow.Description))
                {
                    result.AppendLine($"**説明:** {workflow.Description}");
                }
                
                if (workflow.Tags.Count > 0)
                {
                    result.AppendLine($"**対象:** {string.Join(", ", workflow.Tags)}");
                }
                result.AppendLine();

                if (workflow.Steps.Count > 0)
                {
                    result.AppendLine("**実行手順:**");
                    for (int i = 0; i < workflow.Steps.Count; i++)
                    {
                        var step = workflow.Steps[i];
                        result.AppendLine($"{i + 1}. **{step.ToolName}** - {step.Description}");
                        
                        if (step.Parameters != null && HasParameters(step.Parameters))
                        {
                            result.AppendLine($"   パラメータ: {System.Text.Json.JsonSerializer.Serialize(step.Parameters, new System.Text.Json.JsonSerializerOptions { WriteIndented = false, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping })}");
                        }
                    }
                }
                result.AppendLine();
                result.AppendLine("---");
                result.AppendLine();
            }

            return new StandardResponse
            {
                Success = true,
                FormattedOutput = result.ToString()
            };
        }
    }

    /// <summary>ワークフロー提案</summary>
    public class WorkflowSuggestion
    {
        public string Tool { get; set; }
        public string Reason { get; set; }
        public string Priority { get; set; }
        public object Parameters { get; set; }
        public string WorkflowName { get; set; }
    }

    /// <summary>ワークフローパターン</summary>
    public class WorkflowPattern
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public WorkflowStep[] Steps { get; set; }
        public string[] UseCases { get; set; }
    }

    /// <summary>ワークフローステップ</summary>
    public class WorkflowStep
    {
        public int Order { get; set; }
        public string Tool { get; set; }
        public string Description { get; set; }
        public string Parameters { get; set; }
    }
}
