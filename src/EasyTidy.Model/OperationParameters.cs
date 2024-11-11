using CommunityToolkit.WinUI;
using System;
using System.Collections.Generic;

namespace EasyTidy.Model;

public class OperationParameters
{
    public OperationMode OperationMode { get; set; }

    public string SourcePath { get; set; }

    public string TargetPath { get; set; }

    public FileOperationType FileOperationType { get; set; }

    public List<Func<string, bool>> Funcs { get; set; }

    public RuleModel RuleModel { get; set; }

    public bool HandleSubfolders { get; set; }

    public Func<string, bool> PathFilter { get; set; }

    public string RuleName { get; set; }

    public string OldTargetPath { get; set; }

    public string OldSourcePath { get; set; }

    public OperationParameters(OperationMode operationMode, string sourcePath, string targetPath, FileOperationType fileOperationType, bool handleSubfolders,
        List<Func<string, bool>> funcs, Func<string, bool> pathFilter, RuleModel ruleModel = null)
    {
        OperationMode = operationMode;
        RuleModel = ruleModel;
        SourcePath = sourcePath;
        TargetPath = targetPath;
        FileOperationType = fileOperationType;
        HandleSubfolders = handleSubfolders;
        Funcs = funcs;
        PathFilter = pathFilter;
    }

    public static OperationParameters CreateOperationParameters(OperationParameters parameter)
    {
        return new OperationParameters(
            parameter.OperationMode,
            parameter.SourcePath = parameter.SourcePath.Equals("DesktopText".GetLocalized()) 
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) 
            : parameter.SourcePath,
            parameter.TargetPath,
            parameter.FileOperationType,
            parameter.HandleSubfolders,
            parameter.Funcs,
            parameter.PathFilter,
            parameter.RuleModel
        );
    }

}
