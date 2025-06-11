using System;

namespace EasyTidy.Model;

public class OrchestrationTask
{
    public string? GroupName { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string Rule { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string? SourcePath { get; set; }

    public TaskOrchestrationTable ToEntity(TaskGroupTable groupTable, Guid aiIdentify = default)
    {
        return new TaskOrchestrationTable
        {
            GroupName = groupTable,
            TaskName = TaskName,
            TaskRule = Rule,
            OperationMode = Enum.TryParse<OperationMode>(Action, out var actionMode) ? actionMode : default,
            TaskTarget = TargetPath,
            TaskSource = SourcePath,
            IsEnabled = true,
            IsRegex = false,
            Shortcut = false,
            RuleType = TaskRuleType.CustomRule,
            Filter = null,
            Priority = 5,
            CreateTime = DateTime.Now,
            Argument = string.Empty,
            AutomaticTable = null,
            AIIdentify = aiIdentify,
            UserDefinePromptsJson = string.Empty
        };
    }
}
