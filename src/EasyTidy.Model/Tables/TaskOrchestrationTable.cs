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
    /// 是否已关联
    /// </summary>
    public bool IsRelated { get; set; } = false;

    public TaskGroupTable GroupName { get; set; }

}
