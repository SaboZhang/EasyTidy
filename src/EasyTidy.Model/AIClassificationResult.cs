using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Model;

public class AIClassificationResult
{
    public OperationMode OperationMode { get; set; }
    public string? SourcePath { get; set; }
    public string? TargetPath { get; set; }
    public string? Rule { get; set; }
    public string? Filter { get; set; }
    public string? Content { get; set; }
    public bool IsIncluded { get; set; }
    public string RawResult { get; set; } = string.Empty;

}
