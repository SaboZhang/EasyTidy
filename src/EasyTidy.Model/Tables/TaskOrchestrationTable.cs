using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyTidy.Model;

[Table("TaskOrchestration")]
public class TaskOrchestrationTable
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID { get; set; }

    public string TaskName { get; set; }

    public string TaskRule { get; set; }

    public string TaskSource { get; set; }

    public bool Shortcut { get; set; }

    public string TaskTarget { get; set; }

    public OperationMode OperationMode { get; set; }

    public bool IsEnabled { get; set; }
    /// <summary>
    /// 是否正则
    /// </summary>
    public bool IsRegex { get; set; } = false;

    /// <summary>
    /// 是否已关联
    /// </summary>
    public bool IsRelated { get; set; } = false;

    public TaskRuleType RuleType { get; set; }

    public TaskGroupTable GroupName { get; set; }

    public FilterTable Filter { get; set; }

    public int Priority { get; set; } = 5;

    public DateTime CreateTime { get; set; } = DateTime.Now;

    public AutomaticTable AutomaticTable { get; set; }

    [NotMapped]
    public bool TagOrder { get; set; } = false;

    [NotMapped]
    public bool IdOrder
    {
        get => !TagOrder;
        set
        {
            // 更新互斥属性的值
            TagOrder = !value;
        }
    }

}
