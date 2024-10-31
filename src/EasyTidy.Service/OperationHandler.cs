using EasyTidy.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyTidy.Service;

public static class OperationHandler
{
    // 创建委托类型用于操作方法
    private delegate void OperationMethod(OperationParameters parameter);

    // 使用字典映射操作名称到方法
    private static readonly Dictionary<OperationMode, Func<OperationParameters, Task>> _operations;

    static OperationHandler()
    {
        _operations = new Dictionary<OperationMode, Func<OperationParameters, Task>>
        {
            { OperationMode.Move, MoveAsync },
            { OperationMode.Copy, CopyAsync },
            { OperationMode.Delete, DeleteAsync },
            { OperationMode.Rename, RenameAsync },
            { OperationMode.RecycleBin, RecycleBinAsync },
        };
    }

    // 执行操作的方法
    public static async Task ExecuteOperationAsync(OperationMode operationValue, OperationParameters parameter)
    {
        if (Enum.IsDefined(typeof(OperationMode), operationValue))
        {
            var operation = operationValue;
            // 查找并执行对应的方法
            if (_operations.TryGetValue(operation, out var operationMethod))
            {
                await operationMethod(parameter);
            }
            else
            {
                Console.WriteLine("未找到对应的方法");
            }
        }
        else
        {
            Console.WriteLine("无效的操作值");
        }
    }

    // 操作方法示例
    private static async Task MoveAsync(OperationParameters parameter)
    {
        await Task.Run(() =>
        {
            FileActuator.ExecuteFileOperation(OperationMode.Move, parameter.SourcePath, parameter.TargetPath, parameter.FileOperationType, (bool)parameter.HandleSubfolders, parameter.Funcs);
        });
        Console.WriteLine("执行移动操作");
    }

    private static async Task CopyAsync(OperationParameters parameter)
    {
        await Task.Run(() =>
        {
            FileActuator.ExecuteFileOperation(OperationMode.Copy, parameter.SourcePath, parameter.TargetPath, parameter.FileOperationType, (bool)parameter.HandleSubfolders, parameter.Funcs);
        });
        Console.WriteLine("执行复制操作");
    }

    private static async Task DeleteAsync(OperationParameters parameter)
    {
        await Task.Run(() =>
        {
            FileActuator.ExecuteFileOperation(OperationMode.Delete, parameter.SourcePath, parameter.TargetPath, parameter.FileOperationType, (bool)parameter.HandleSubfolders, parameter.Funcs);
        });
        Console.WriteLine("执行删除操作");
    }

    private static async Task RenameAsync(OperationParameters parameter)
    {
        await Task.Run(() =>
        {
            FileActuator.ExecuteFileOperation(OperationMode.Rename, parameter.SourcePath, parameter.TargetPath, parameter.FileOperationType, (bool)parameter.HandleSubfolders, parameter.Funcs);
        });
        Console.WriteLine("重命名逻辑");
    }

    private static async Task RecycleBinAsync(OperationParameters parameter) 
    {
        await Task.Run(() => 
        { 
            FileActuator.ExecuteFileOperation(OperationMode.RecycleBin, parameter.SourcePath, parameter.TargetPath, parameter.FileOperationType, (bool)parameter.HandleSubfolders, parameter.Funcs);
        });
        Console.WriteLine("回收站逻辑");
    }
}
