using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Model;

public class AutomaticConfigModel
{
    public bool IsFileChange { get; set;} = false;

    public bool IsStartupExecution { get; set; } = false;

    public bool RegularTaskRunning { get; set; } = false;

    public bool OnScheduleExecution { get; set; } = false;
}
