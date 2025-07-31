using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace uMCP.Editor.Tools
{
    /// <summary>MarkdownファイルからワークフローをパースするUtility</summary>
    public static class WorkflowMarkdownParser
    {
        /// <summary>ワークフローディレクトリのパス</summary>
        public static string WorkflowDirectory => Path.Combine(Application.dataPath, "uMcp/Workflows");

        /// <summary>すべてのワークフローMarkdownファイルを読み込み</summary>
        public static List<ParsedWorkflow> LoadAllWorkflows()
        {
            var workflows = new List<ParsedWorkflow>();

            if (!Directory.Exists(WorkflowDirectory))
            {
                Debug.LogWarning($"Workflow directory not found: {WorkflowDirectory}");
                return workflows;
            }

            var mdFiles = Directory.GetFiles(WorkflowDirectory, "*.md");
            foreach (var file in mdFiles)
            {
                try
                {
                    var workflow = ParseWorkflowFile(file);
                    if (workflow != null)
                    {
                        workflows.Add(workflow);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to parse workflow file {file}: {ex.Message}");
                }
            }

            return workflows;
        }

        /// <summary>単一のワークフローファイルをパース</summary>
        public static ParsedWorkflow ParseWorkflowFile(string filePath)
        {
            var content = File.ReadAllText(filePath);
            var workflow = new ParsedWorkflow
            {
                FileName = Path.GetFileName(filePath),
                Steps = new List<WorkflowStepInfo>(),
                Tags = new List<string>(),
                TriggerConditions = new List<string>()
            };

            var lines = content.Split('\n');
            var currentSection = "";
            WorkflowStepInfo currentStep = null;
            var inParametersSection = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // セクションヘッダーの検出
                if (trimmedLine.StartsWith("# "))
                {
                    workflow.Name = trimmedLine.Substring(2);
                    continue;
                }

                if (trimmedLine.StartsWith("## "))
                {
                    currentSection = trimmedLine.Substring(3).ToLower();
                    inParametersSection = false;
                    continue;
                }

                if (trimmedLine.StartsWith("### ") && currentSection == "ステップ")
                {
                    // 新しいステップの開始
                    if (currentStep != null)
                    {
                        workflow.Steps.Add(currentStep);
                    }
                    currentStep = new WorkflowStepInfo
                    {
                        StepName = trimmedLine.Substring(4),
                        Parameters = new Dictionary<string, object>()
                    };
                    inParametersSection = false;
                    continue;
                }

                // 各セクションの内容をパース
                if (currentSection == "概要" && !string.IsNullOrWhiteSpace(trimmedLine))
                {
                    workflow.Description = trimmedLine;
                }
                else if (currentSection == "タグ" && trimmedLine.StartsWith("- "))
                {
                    workflow.Tags.Add(trimmedLine.Substring(2));
                }
                else if (currentSection == "トリガー条件" && trimmedLine.StartsWith("- "))
                {
                    workflow.TriggerConditions.Add(trimmedLine.Substring(2));
                }
                else if (currentSection == "ステップ" && currentStep != null)
                {
                    ParseStepLine(trimmedLine, currentStep, ref inParametersSection);
                }
            }

            // 最後のステップを追加
            if (currentStep != null)
            {
                workflow.Steps.Add(currentStep);
            }

            return workflow;
        }

        /// <summary>ステップの行をパース</summary>
        static void ParseStepLine(string line, WorkflowStepInfo step, ref bool inParametersSection)
        {
            if (line.StartsWith("- tool:"))
            {
                step.ToolName = line.Substring(8).Trim();
            }
            else if (line.StartsWith("- 説明:"))
            {
                step.Description = line.Substring(7).Trim();
            }
            else if (line.StartsWith("- 必須:"))
            {
                step.IsRequired = line.Contains("true");
            }
            else if (line.StartsWith("- 条件:"))
            {
                step.Condition = line.Substring(7).Trim();
            }
            else if (line.StartsWith("- パラメータ:"))
            {
                inParametersSection = true;
            }
            else if (inParametersSection && line.StartsWith("  - "))
            {
                // パラメータのパース
                var paramMatch = Regex.Match(line, @"  - (\w+):\s*(.+)");
                if (paramMatch.Success)
                {
                    var key = paramMatch.Groups[1].Value;
                    var value = paramMatch.Groups[2].Value;

                    // 値の型を推測
                    if (value == "true" || value == "false")
                    {
                        step.Parameters[key] = bool.Parse(value);
                    }
                    else if (int.TryParse(value, out var intVal))
                    {
                        step.Parameters[key] = intVal;
                    }
                    else
                    {
                        step.Parameters[key] = value;
                    }
                }
            }
        }

        /// <summary>トリガー定義をパース</summary>
        public static WorkflowTriggers ParseTriggerFile()
        {
            var triggerFile = Path.Combine(WorkflowDirectory, "workflow-triggers.md");
            if (!File.Exists(triggerFile))
            {
                return new WorkflowTriggers();
            }

            var triggers = new WorkflowTriggers
            {
                ToolBasedTriggers = new Dictionary<string, List<TriggerAction>>(),
                ContextBasedTriggers = new Dictionary<string, List<string>>()
            };

            var content = File.ReadAllText(triggerFile);
            var lines = content.Split('\n');
            var currentTool = "";
            var currentSection = "";

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("#### ") && trimmedLine.Contains("実行後"))
                {
                    var toolMatch = Regex.Match(trimmedLine, @"#### (\w+) 実行後");
                    if (toolMatch.Success)
                    {
                        currentTool = toolMatch.Groups[1].Value;
                        triggers.ToolBasedTriggers[currentTool] = new List<TriggerAction>();
                    }
                }
                else if (trimmedLine.StartsWith("#### キーワード:"))
                {
                    currentSection = "context";
                }
                else if (trimmedLine.StartsWith("- 推奨:") && !string.IsNullOrEmpty(currentTool))
                {
                    var match = Regex.Match(trimmedLine, @"- 推奨: (\w+) \((\w+)\)");
                    if (match.Success)
                    {
                        triggers.ToolBasedTriggers[currentTool].Add(new TriggerAction
                        {
                            Tool = match.Groups[1].Value,
                            Priority = match.Groups[2].Value
                        });
                    }
                }
            }

            return triggers;
        }
    }

    /// <summary>パースされたワークフロー</summary>
    public class ParsedWorkflow
    {
        public string FileName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; set; }
        public List<string> TriggerConditions { get; set; }
        public List<WorkflowStepInfo> Steps { get; set; }
    }

    /// <summary>ワークフローステップ情報</summary>
    public class WorkflowStepInfo
    {
        public string StepName { get; set; }
        public string ToolName { get; set; }
        public string Description { get; set; }
        public bool IsRequired { get; set; }
        public string Condition { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }

    /// <summary>ワークフロートリガー定義</summary>
    public class WorkflowTriggers
    {
        public Dictionary<string, List<TriggerAction>> ToolBasedTriggers { get; set; }
        public Dictionary<string, List<string>> ContextBasedTriggers { get; set; }
    }

    /// <summary>トリガーアクション</summary>
    public class TriggerAction
    {
        public string Tool { get; set; }
        public string Priority { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
}
