using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyTidy.Model;

[Table("Automatic")]
public class AutomaticTable
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID { get; set; }

    public bool IsFileChange { get; set; } = false;

    public string DelaySeconds { get; set; } = "5";

    public bool RegularTaskRunning { get; set; } = false;

    public string Hourly { get; set; } = "00";

    public string Minutes { get; set; } = "00";

    public bool IsStartupExecution { get; set; } = false;

    public bool OnScheduleExecution { get; set; } = false;

    public ScheduleTable Schedule { get; set; }

    public List<FileExplorerTable> FileExplorerList { get; set; }

}

