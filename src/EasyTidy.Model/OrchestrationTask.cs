using CommunityToolkit.WinUI;
using System;
using System.ComponentModel.DataAnnotations;

namespace EasyTidy.Model;

public class OrchestrationTask
{
    [Display(Name = "TaskGroupName")]
    public string? GroupName { get; set; }

    [Required]
    [Display(Name = "TaskName")]
    public string TaskName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ProcessingRules")]
    public string Rule { get; set; } = string.Empty;

    [Required]
    [Display(Name = "OperatingMode")]
    public string Action { get; set; } = string.Empty;

    [Required]
    [Display(Name = "TargetPath")]
    public string TargetPath { get; set; } = string.Empty;
    [Display(Name = "SourcePath")]
    public string? SourcePath { get; set; }

    [Display(Name = "IsItRegular")]
    public string IsRegex { get; set; } = "N";

    public TaskOrchestrationTable ToEntity(TaskGroupTable groupTable)
    {
        if (string.IsNullOrEmpty(TaskName))
        {
            throw new ArgumentException(string.Format("ParameterValidation".GetLocalized(), "TaskName".GetLocalized()));
        }
        if (string.IsNullOrEmpty(Rule))
        {
            throw new ArgumentException(string.Format("ParameterValidation".GetLocalized(), "ProcessingRules".GetLocalized()));
        }
        if (string.IsNullOrEmpty(Action))
        {
            throw new ArgumentException(string.Format("ParameterValidation".GetLocalized(), "OperatingMode".GetLocalized()));
        }
        if (string.IsNullOrEmpty(TargetPath))
        {
            throw new ArgumentException(string.Format("ParameterValidation".GetLocalized(), "TargetPath".GetLocalized()));
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
