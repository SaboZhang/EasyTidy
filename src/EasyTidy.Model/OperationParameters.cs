using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Model;

public class OperationParameters
{
    public int Id { get; set; }
    public OperationMode OperationMode { get; set; }

    public string SourcePath { get; set; }

    public string TargetPath { get; set; }

    public FileOperationType FileOperationType { get; set; }

    public List<Func<string, bool>> Funcs { get; set; }

    public RuleModel RuleModel { get; set; }

    public bool HandleSubfolders { get; set; }

    public Func<string, bool> PathFilter { get; set; }

    public string RuleName { get; set; }

    public OperationParameters(OperationMode operationMode, string sourcePath, string targetPath, FileOperationType fileOperationType, bool handleSubfolders, 
        List<Func<string, bool>> funcs, Func<string, bool> pathFilter, RuleModel ruleModel = null, int id = 0, string ruleName = null)
    {
        OperationMode = operationMode;
        RuleModel = ruleModel;
        SourcePath = sourcePath;
        TargetPath = targetPath;
        FileOperationType = fileOperationType;
        HandleSubfolders = handleSubfolders;
        Funcs = funcs;
        PathFilter = pathFilter;
        Id = id;
        RuleName = ruleName;
    }

}
