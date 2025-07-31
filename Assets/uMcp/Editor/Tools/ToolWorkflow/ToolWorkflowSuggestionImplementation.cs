using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using uMCP.Editor.Core.Attributes;
using UnityEditor;
using UnityEngine;

namespace uMCP.Editor.Tools
{
    /// <summary>MCPツールの連携ワークフロー提案システム</summary>
    [McpServerToolType, Description("MCPツール連携とワークフロー提案")]
    internal sealed class ToolWorkflowSuggestionImplementation
    {
        /// <summary>現在のコンテキストから次の推奨アクションを提案</summary>
        [McpServerTool, Description("現在の状態から推奨される次のMCPツール実行を提案")]
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

            return new
            {
                Success = true,
                LastTool = lastExecutedTool ?? "none",
                Context = workContext ?? "general",
                Suggestions = suggestions.OrderByDescending(s => GetPriorityValue(s.Priority)).ToList(),
                TotalSuggestions = suggestions.Count,
                Source = "Markdown-based workflow system"
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
        [McpServerTool, Description("Markdownファイルから読み込んだワークフローパターンを取得")]
        public async ValueTask<object> GetWorkflowPatterns()
        {
            await UniTask.SwitchToMainThread();

            var workflows = WorkflowMarkdownParser.LoadAllWorkflows();
            var patterns = new List<WorkflowPattern>();

            foreach (var workflow in workflows)
            {
                var pattern = new WorkflowPattern
                {
                    Name = workflow.Name,
                    Description = workflow.Description,
                    Steps = workflow.Steps.Select((step, index) => new WorkflowStep
                    {
                        Order = index + 1,
                        Tool = step.ToolName,
                        Description = step.Description,
                        Parameters = step.Parameters != null ? 
                            System.Text.Json.JsonSerializer.Serialize(step.Parameters) : null
                    }).ToArray(),
                    UseCases = workflow.Tags.ToArray()
                };
                patterns.Add(pattern);
            }

            return new
            {
                Success = true,
                Patterns = patterns,
                TotalPatterns = patterns.Count,
                WorkflowDirectory = WorkflowMarkdownParser.WorkflowDirectory,
                Message = "Markdownファイルからワークフローパターンを読み込みました"
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