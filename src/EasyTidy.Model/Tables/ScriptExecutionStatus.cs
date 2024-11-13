
using System;

namespace EasyTidy.Model;

public class ScriptExecutionStatus
{
    public int Id { get; set; }
    public string ScriptName { get; set; }
    public string Status { get; set; }
    public DateTime ExecutionDate { get; set; }
}