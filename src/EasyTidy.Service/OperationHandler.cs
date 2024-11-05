using EasyTidy.Log;
using EasyTidy.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasyTidy.Service;

public static class OperationHandler
{
    private static readonly HashSet<string> _executedOperations = new HashSet<string>();

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

            var localSemaphore = new SemaphoreSlim(1, 1);
            await localSemaphore.WaitAsync();
            // 查找并执行对应的方法
            try
            {
                // 查找并执行对应的方法
                if (_operations.TryGetValue(operation, out var operationMethod))
                {
                    // 执行操作
                    await operationMethod(parameter);
                }
                else
                {
                    LogService.Logger.Error("未找到对应的方法");
                }
            }
            catch (Exception ex)
            {
                LogService.Logger.Error($"Error executing operation: {ex.Message}");
            }
            finally
            {
                localSemaphore.Release(); // 确保释放锁
            }
        }
        else
        {
            LogService.Logger.Error("无效的操作值");
        }
    }

    // 操作方法示例
    private static async Task MoveAsync(OperationParameters parameter)
    {
        string operationId = $"{parameter.SourcePath}-{parameter.TargetPath}-{parameter.FileOperationType}";

        // 使用锁来确保同一时间只有一个线程可以执行该操作
        lock (_executedOperations)
        {
            if (_executedOperations.Contains(operationId))
            {
                LogService.Logger.Info($"Move operation already in progress, skipping execution. {parameter.Id}");
                return;
            }
            _executedOperations.Add(operationId);
        }

        try
        {
            await Task.Run(async () =>
            {
                // 确保传递 OperationMode 参数
                await FileActuator.ExecuteFileOperationAsync(parameter);
            });

            LogService.Logger.Info("执行移动操作完成");
        }
        catch (Exception ex)
        {
            LogService.Logger.Info($"执行移动操作异常 {ex}");
        }
        finally
        {
            lock (_executedOperations)
            {
                _executedOperations.Remove(operationId);
                LogService.Logger.Info("Removed operation from executed operations.");
            }
        }
    }

    private static async Task CopyAsync(OperationParameters parameter)
    {
        await Task.Run(async () =>
        {
            await FileActuator.ExecuteFileOperationAsync(parameter);
        });
        LogService.Logger.Info("执行复制操作");
    }

    private static async Task DeleteAsync(OperationParameters parameter)
    {
        await Task.Run(async () =>
        {
            await FileActuator.ExecuteFileOperationAsync(parameter);
        });
        LogService.Logger.Info("执行删除操作");
    }

    private static async Task RenameAsync(OperationParameters parameter)
    {
        await Task.Run(async () =>
        {
            await FileActuator.ExecuteFileOperationAsync(parameter);
        });
        LogService.Logger.Info("重命名逻辑");
    }

    private static async Task RecycleBinAsync(OperationParameters parameter)
    {
        string operationId = $"{parameter.SourcePath}-{parameter.TargetPath}-{parameter.FileOperationType}";

        // 使用锁来确保同一时间只有一个线程可以执行该操作
        lock (_executedOperations)
        {
            if (_executedOperations.Contains(operationId))
            {
                LogService.Logger.Info($"Move operation already in progress, skipping execution. {parameter.Id}");
                return;
            }
            _executedOperations.Add(operationId);
        }
        try
        {
            await Task.Run(async () =>
            {
                await FileActuator.ProcessFileAsync(parameter);
            });
            LogService.Logger.Info("回收站逻辑");
        }
        finally
        {
            lock (_executedOperations)
            {
                _executedOperations.Remove(operationId);
                LogService.Logger.Info("Removed operation from executed operations.");
            }
        }
        
    }
}
