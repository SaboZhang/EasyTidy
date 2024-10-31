using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Model;

public class OperationParameters
{

    public OperationMode OperationMode { get; set; }

    public string SourcePath { get; set; }

    public string TargetPath { get; set; }

    public FileOperationType FileOperationType { get; set; }

    public List<Func<string, bool>> Funcs { get; set; }

    public RuleModel RuleModel { get; set; }

    public bool? HandleSubfolders { get; set; }

}
