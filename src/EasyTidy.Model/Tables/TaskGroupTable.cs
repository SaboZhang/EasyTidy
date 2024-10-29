using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyTidy.Model;

[Table("TaskGroup")]
public class TaskGroupTable
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string GroupName { get; set; }

    public bool IsUsed { get; set; } = false;

    public List<TaskOrchestrationTable> TaskOrchestrationList { get; set; }

}
