using EasyTidy.Log;
using EasyTidy.Model;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Service;

public static class FileActuator
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _operationLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

    public static async Task ExecuteFileOperationAsync(OperationParameters parameters, int maxRetries = 5)
    {
        string operationId = $"{parameters.OperationMode}-{parameters.SourcePath}-{Guid.NewGuid()}";
        var operationLock = _operationLocks.GetOrAdd(operationId, _ => new SemaphoreSlim(1, 1));

        await operationLock.WaitAsync();
        try
        {

            if (Path.GetExtension(parameters.SourcePath).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await RetryAsync(maxRetries, async () =>
            {

                if (Directory.Exists(parameters.SourcePath))
                {
                    await ProcessDirectoryAsync(parameters);
                }
                else if (File.Exists(parameters.SourcePath))
                {
                    await ProcessFileAsync(parameters);
                }
                else
                {
                    LogService.Logger.Error($"Source path '{parameters.SourcePath}' not found.");
                    throw new FileNotFoundException(parameters.SourcePath);
                }
            });
        }
        catch (Exception ex)
        {
            LogService.Logger.Error($"Error executing file operation: {ex.Message}");
        }
        finally
        {
            operationLock.Release();
            _operationLocks.TryRemove(operationId, out _);
            LogService.Logger.Info("Removed operation lock from executed operations.");
        }
    }

    private static async Task RetryAsync(int maxRetries, Func<Task> action)
    {
        int attempt = 0;
        while (attempt < maxRetries)
        {
            try
            {
                await action();
                return; // 成功后退出
            }
            catch (Exception ex)
            {
                attempt++;
                LogService.Logger.Error($"Error during operation: {ex.Message}. Attempt {attempt}/{maxRetries}");
                if (attempt >= maxRetries) throw; // 达到最大重试次数后抛出异常
                await Task.Delay(1000); // 可选延迟
            }
        }
    }

    private static async Task ProcessDirectoryAsync(OperationParameters parameters)
    {
        // 创建目标目录
        if (!Directory.Exists(parameters.TargetPath))
        {
            Directory.CreateDirectory(parameters.TargetPath);
        }

        var fileList = Directory.GetFiles(parameters.SourcePath).ToList();
        // 获取所有文件并处理
        int fileCount = 0;
        foreach (var filePath in fileList)
        {
            fileCount++;
            var dynamicFilters = new List<Func<string, bool>>(parameters.Funcs);
            if (ShouldSkip(new List<Func<string, bool>>(parameters.Funcs), filePath, parameters.PathFilter))
            {
                LogService.Logger.Info($"执行文件操作，获取所有文件跳过不符合条件的文件 filePath: {filePath}, RuleFilters: {parameters.RuleName},=== {fileCount}, sourcePath: {parameters.SourcePath}");
                continue; // 跳过不符合条件的文件
            }

            // 更新目标路径
            var targetFilePath = Path.Combine(parameters.TargetPath, Path.GetFileName(filePath));
            var fileParameters = new OperationParameters(
                parameters.OperationMode,
                filePath,
                targetFilePath,
                parameters.FileOperationType,
                parameters.HandleSubfolders,
                parameters.Funcs,
                parameters.PathFilter
                );

            await ProcessFileAsync(fileParameters);
        }

        // 递归处理子文件夹
        if (parameters.HandleSubfolders)
        {
            var subDirList = Directory.GetDirectories(parameters.SourcePath).ToList();
            foreach (var subDir in subDirList)
            {
                if (ShouldSkip(parameters.Funcs, subDir, parameters.PathFilter))
                {
                    continue; // 跳过不符合条件的文件夹
                }

                // 为子文件夹生成新的目标路径
                var newTargetPath = Path.Combine(parameters.TargetPath, Path.GetFileName(subDir));
                LogService.Logger.Info($"执行文件操作，递归处理子文件夹: {newTargetPath}");

                // 递归调用，传递新的目标路径
                await ProcessDirectoryAsync(new OperationParameters(
                    parameters.OperationMode,
                    subDir,
                    newTargetPath,
                    parameters.FileOperationType,
                    parameters.HandleSubfolders,
                    parameters.Funcs,
                    parameters.PathFilter
                    ));
            }
        }
    }

    internal static async Task ProcessFileAsync(OperationParameters parameters)
    {
        if (ShouldSkip(parameters.Funcs, parameters.SourcePath, parameters.PathFilter) && parameters.OperationMode != OperationMode.RecycleBin)
        {
            LogService.Logger.Info($"执行文件操作 ShouldSkip {parameters.RuleName}{parameters.Id}");
            return;
        }

        switch (parameters.OperationMode)
        {
            case OperationMode.Move:
                LogService.Logger.Info($"执行文件操作 ProcessFileAsync {parameters.RuleName}{parameters.Id}");
                await MoveFile(parameters.SourcePath, parameters.TargetPath, parameters.FileOperationType);
                break;
            case OperationMode.Copy:
                await CopyFile(parameters.SourcePath, parameters.TargetPath, parameters.FileOperationType);
                break;
            case OperationMode.Delete:
                await DeleteFile(parameters.TargetPath);
                break;
            case OperationMode.Rename:
                await RenameFile(parameters.SourcePath, parameters.TargetPath);
                break;
            case OperationMode.RecycleBin:
                await MoveToRecycleBin(parameters.TargetPath, new List<Func<string, bool>>(parameters.Funcs), 
                    parameters.PathFilter, parameters.RuleModel.RuleType, parameters.HandleSubfolders);
                break;
            default:
                throw new NotSupportedException($"Operation mode '{parameters.OperationMode}' is not supported.");
        }
    }

    private static async Task MoveFile(string sourcePath, string targetPath, FileOperationType fileOperationType)
    {
        await _semaphore.WaitAsync(); // 请求对文件操作的独占访问
        try
        {
            HandleFileConflict(sourcePath, targetPath, fileOperationType, () =>
            {
                File.Move(sourcePath, targetPath);
            });
        }
        catch (Exception ex)
        {
            // 处理异常（记录日志等）
            LogService.Logger.Error($"Error moving file: {ex.Message}");
        }
        finally
        {
            _semaphore.Release(); // 确保释放互斥锁
        }
    }

    private static async Task CopyFile(string sourcePath, string targetPath, FileOperationType fileOperationType)
    {
        await _semaphore.WaitAsync(); // 请求对文件操作的独占访问
        try
        {
            HandleFileConflict(sourcePath, targetPath, fileOperationType, () =>
            {
                File.Copy(sourcePath, targetPath);
            });

        }
        catch (Exception ex)
        {
            // 处理异常（记录日志等）
            LogService.Logger.Error($"Error copying file: {ex.Message}");
        }
        finally
        {
            _semaphore.Release(); // 确保释放互斥锁
        }
    }

    private static async Task DeleteFile(string path)
    {
        await _semaphore.WaitAsync(); // 请求对文件操作的独占访问
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            // 处理异常（记录日志等）
            LogService.Logger.Error($"Error deleting file: {ex.Message}");
        }
        finally
        {
            _semaphore.Release(); // 确保释放互斥锁
        }
    }

    private static async Task RenameFile(string sourcePath, string targetPath)
    {
        await _semaphore.WaitAsync(); // 请求对文件操作的独占访问
        try
        {
            File.Move(sourcePath, targetPath);

        }
        catch (Exception ex)
        {
            // 处理异常（记录日志等）
            LogService.Logger.Error($"Error renaming file: {ex.Message}");
        }
        finally
        {
            _semaphore.Release(); // 确保释放互斥锁
        }
    }

    private static async Task MoveToRecycleBin(string path, List<Func<string, bool>> dynamicFilters, 
        Func<string, bool>? pathFilter, TaskRuleType ruleType, bool deleteSubfolders = false)
    {
        await _semaphore.WaitAsync(); // 请求对文件操作的独占访问
        try
        {
            if (File.Exists(path)) // 如果路径是文件
            {
                FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin); // 将文件移到回收站
            }
            else if (Directory.Exists(path)) // 如果路径是文件夹
            {
                if (deleteSubfolders)
                {
                    foreach (var subfolder in Directory.GetDirectories(path))
                    {
                        if (ShouldSkip(dynamicFilters, path, pathFilter) && ruleType != TaskRuleType.FileRule)
                        {
                            continue;
                        }
                        await MoveToRecycleBin(subfolder, dynamicFilters, pathFilter, ruleType, true); // 递归处理子文件夹
                        // 将当前目录移到回收站（在子文件夹中文件处理完成后进行）
                        FileSystem.DeleteDirectory(subfolder, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    }
                    
                }

                // 获取目录中的所有文件并移到回收站
                var files = Directory.GetFiles(path);
                foreach (var file in files)
                {
                    if (ShouldSkip(dynamicFilters, path, pathFilter) && ruleType != TaskRuleType.FolderRule)
                    {
                        continue;
                    }
                    FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
                
            }
            else
            {
                throw new FileNotFoundException($"File or directory not found: {path}");
            }
        }
        catch (Exception ex)
        {
            // 处理异常（记录日志等）
            LogService.Logger.Error($"Error moving to recycle bin: {ex.Message}");
        }
        finally
        {
            _semaphore.Release(); // 确保释放互斥锁
        }
    }

    private static void HandleFileConflict(string sourcePath, string targetPath, FileOperationType fileOperationType, Action action)
    {
        if (File.Exists(targetPath))
        {
            switch (fileOperationType)
            {
                case FileOperationType.Skip:
                    // Do nothing
                    break;
                case FileOperationType.Override:
                    action(); // 执行操作
                    break;
                case FileOperationType.OverwriteIfNewer:
                    if (File.GetLastWriteTime(sourcePath) > File.GetLastWriteTime(targetPath))
                    {
                        action(); // 执行操作
                    }
                    break;
                case FileOperationType.OverrideIfSizesDiffer:
                    if (new FileInfo(sourcePath).Length != new FileInfo(targetPath).Length)
                    {
                        action(); // 执行操作
                    }
                    break;
                case FileOperationType.ReNameAppend:
                    string newPath = GetUniqueFilePath(targetPath);
                    File.Copy(sourcePath, newPath);
                    break;
                case FileOperationType.ReNameAddDate:
                    newPath = GetUniqueFilePathWithDate(targetPath);
                    File.Copy(sourcePath, newPath);
                    break;
                default:
                    throw new NotSupportedException($"File operation type '{fileOperationType}' is not supported.");
            }
        }
        else
        {
            action(); // 如果目标文件不存在，直接执行操作
        }
    }

    private static string GetUniqueFilePath(string path)
    {
        string directory = Path.GetDirectoryName(path);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        int count = 1;

        string newFilePath = path;
        while (File.Exists(newFilePath))
        {
            newFilePath = Path.Combine(directory, $"{fileNameWithoutExtension}_{count}{extension}");
            count++;
        }

        return newFilePath;
    }

    private static string GetUniqueFilePathWithDate(string path)
    {
        string directory = Path.GetDirectoryName(path);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        string newFileName = $"{fileNameWithoutExtension}_{timestamp}{extension}";

        return Path.Combine(directory, newFileName);
    }

    private static bool ShouldSkip(List<Func<string, bool>> dynamicFilters, string path, Func<string, bool>? pathFilter)
    {
        // 检查是否是快捷方式
        if (Path.GetExtension(path).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            return true; // 跳过快捷方式文件
        }

        // 规则1检查：dynamicFilters 列表中满足任意一个条件
        bool satisfiesDynamicFilters = dynamicFilters != null && dynamicFilters.Any(filter => filter(path));

        // 规则2检查：如果 pathFilter 不为 null，则它应返回 true
        bool satisfiesPathFilter = pathFilter == null || pathFilter(path);

        // 如果 pathFilter 为 null，仅根据 satisfiesDynamicFilters 的结果返回
        if (pathFilter == null)
        {
            LogService.Logger.Info($"satisfiesDynamicFilters (no pathFilter): {satisfiesDynamicFilters}");
            return !satisfiesDynamicFilters;
        }

        // 如果 pathFilter 不为 null，要求 satisfiesDynamicFilters 和 satisfiesPathFilter 同时满足
        LogService.Logger.Info($"satisfiesDynamicFilters: {satisfiesDynamicFilters}, satisfiesPathFilter: {satisfiesPathFilter}");
        return satisfiesDynamicFilters && satisfiesPathFilter;
    }

}
