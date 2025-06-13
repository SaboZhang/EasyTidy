using System;
using System.ComponentModel.DataAnnotations;

namespace EasyTidy.Model;

public class OrchestrationTask
{
    [Display(Name = "任务组名")]
    public string? GroupName { get; set; }

    [Required]
    [Display(Name = "任务名称")]
    public string TaskName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "处理规则")]
    public string Rule { get; set; } = string.Empty;

    [Required]
    [Display(Name = "操作方式")]
    public string Action { get; set; } = string.Empty;

    [Required]
    [Display(Name = "目标路径")]
    public string TargetPath { get; set; } = string.Empty;
    [Display(Name = "源路径")]
    public string? SourcePath { get; set; }

    [Required]
    [Display(Name = "是否正则")]
    public string IsRegex { get; set; } = "N";

    public TaskOrchestrationTable ToEntity(TaskGroupTable groupTable)
    {
        if (string.IsNullOrEmpty(TaskName))
        {
            throw new ArgumentException("TaskName cannot be null or empty.");
        }
        if (string.IsNullOrEmpty(Rule))
        {
            throw new ArgumentException("Rule cannot be null or empty.");
        }
        if (string.IsNullOrEmpty(Action))
        {
            throw new ArgumentException("Action cannot be null or empty.");
        }
        if (string.IsNullOrEmpty(TargetPath))
        {
            throw new ArgumentException("TargetPath cannot be null or empty.");
        }
        if (string.IsNullOrEmpty(IsRegex))
        {
            throw new ArgumentException("IsRegex cannot be null or empty.");
        }
        return new TaskOrchestrationTable
        {
            GroupName = groupTable,
            TaskName = TaskName,
            TaskRule = Rule,
            OperationMode = EnumHelper.TryParseDisplayName<OperationMode>(Action, out var actionMode) ? actionMode : OperationMode.None,
            TaskTarget = TargetPath,
            TaskSource = SourcePath,
            IsEnabled = true,
            IsRegex = string.Equals(IsRegex, "Y", StringComparison.OrdinalIgnoreCase),
            Shortcut = false,
            RuleType = string.Equals(IsRegex, "Y", StringComparison.OrdinalIgnoreCase) ? TaskRuleType.ExpressionRules : TaskRuleType.CustomRule,
            Filter = null,
            Priority = 5,
            CreateTime = DateTime.Now,
            Argument = string.Empty,
            AutomaticTable = null,
            AIIdentify = default,
            UserDefinePromptsJson = string.Empty
        };
    }
}
