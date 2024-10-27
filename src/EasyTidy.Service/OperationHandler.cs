using EasyTidy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Service;

public static class OperationHandler
{
    // 创建委托类型用于操作方法
    private delegate void OperationMethod(string parameter);

    // 使用字典映射操作名称到方法
    private static readonly Dictionary<OperationMode, Func<string, Task>> _operations;

    static OperationHandler()
    {
        _operations = new Dictionary<OperationMode, Func<string, Task>>
        {
            { OperationMode.Move, MoveAsync },
            { OperationMode.Copy, CopyAsync },
            { OperationMode.Delete, DeleteAsync },
            { OperationMode.Rename, RenameAsync },
            { OperationMode.RecycleBin, RecycleBinAsync },
        };
    }

    // 执行操作的方法
    public static async Task ExecuteOperationAsync(int operationValue, string parameter)
    {
        if (Enum.IsDefined(typeof(OperationMode), operationValue))
        {
            var operation = (OperationMode)operationValue;
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
    private static async Task MoveAsync(string parameter)
    {
        await Task.Delay(500);
        Console.WriteLine("执行移动操作");
    }

    private static async Task CopyAsync(string parameter)
    {
        await Task.Delay(500);
        Console.WriteLine("执行复制操作");
    }

    private static async Task DeleteAsync(string parameter)
    {
        await Task.Delay(500);
        Console.WriteLine("执行删除操作");
    }

    private static async Task RenameAsync(string parameter)
    {
        await Task.Delay(500);
        Console.WriteLine("重命名逻辑");
    }

    private static async Task RecycleBinAsync(string parameter) 
    {
        await Task.Delay(500);
        Console.WriteLine("回收站逻辑");
    }
}
