using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Util;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            if (Path.GetExtension(parameters.SourcePath).Equals(".lnk", StringComparison.OrdinalIgnoreCase) 
                || string.IsNullOrEmpty(parameters.TargetPath))
            {
                return;
            }

            await RetryAsync(maxRetries, async () =>
            {
                if (IsFolder(parameters.RuleModel.Rule, parameters.RuleModel.RuleType))
                {
                    await ProcessFoldersAsync(parameters);
                }
                else if (Directory.Exists(parameters.SourcePath))
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

    /// <summary>
    /// 文件夹处理
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    private static async Task ProcessFoldersAsync(OperationParameters parameters)
    {
        if (!Directory.Exists(parameters.TargetPath))
        {
            Directory.CreateDirectory(parameters.TargetPath);
        }

        LogService.Logger.Info($"执行文件夹操作 ProcessFoldersAsync {parameters.TargetPath}");
        var floderList = Directory.GetDirectories(parameters.SourcePath).ToList();

        foreach (var floder in floderList)
        {
            var newParameters = new OperationParameters(
                parameters.OperationMode,
                floder,
                Path.Combine(parameters.TargetPath, Path.GetFileName(floder)),
                parameters.FileOperationType,
                parameters.HandleSubfolders,
                parameters.Funcs,
                parameters.PathFilter);
            await ExecuteFolderOperationAsync(newParameters);
        }
    }

    /// <summary>
    /// 整个文件夹处理
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    private static async Task ExecuteFolderOperationAsync(OperationParameters parameters)
    {
        if (ShouldSkip(parameters.Funcs, parameters.SourcePath, parameters.PathFilter) && parameters.OperationMode != OperationMode.RecycleBin)
        {
            LogService.Logger.Info($"执行文件夹操作 ShouldSkip {parameters.TargetPath}");
            return;
        }

        LogService.Logger.Info($"执行文件夹操作 ExecuteFolderOperationAsync {parameters.TargetPath}, 操作模式: {parameters.OperationMode}");
        switch (parameters.OperationMode)
        {
            case OperationMode.Move:
                await MoveFolder(parameters.SourcePath, parameters.TargetPath, parameters.FileOperationType);
                break;
            case OperationMode.Copy:
                await CopyFile(parameters.SourcePath, parameters.TargetPath, parameters.FileOperationType);
                break;
            case OperationMode.Delete:
                await DeleteFolder(parameters.TargetPath);
                break;
            case OperationMode.Rename:
                await RenameFolder(parameters.SourcePath, parameters.TargetPath);
                break;
            case OperationMode.RecycleBin:
                await MoveToRecycleBin(parameters.TargetPath, new List<Func<string, bool>>(parameters.Funcs),
                    parameters.PathFilter, parameters.RuleModel.RuleType, true, parameters.HandleSubfolders);
                break;
            default:
                throw new NotSupportedException($"Operation mode '{parameters.OperationMode}' is not supported.");
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

    /// <summary>
    /// 处理文件
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    internal static async Task ProcessFileAsync(OperationParameters parameters)
    {
        if (ShouldSkip(parameters.Funcs, parameters.SourcePath, parameters.PathFilter) && parameters.OperationMode != OperationMode.RecycleBin)
        {
            LogService.Logger.Info($"执行文件操作 ShouldSkip {parameters.TargetPath}");
            return;
        }

        LogService.Logger.Info($"执行文件操作 ProcessFileAsync {parameters.TargetPath}, 操作模式: {parameters.OperationMode}");
        switch (parameters.OperationMode)
        {
            case OperationMode.Move:
                await MoveFile(parameters.SourcePath, parameters.TargetPath, parameters.FileOperationType);
                break;
            case OperationMode.Copy:
                await CopyFolder(parameters.SourcePath, parameters.TargetPath, parameters.FileOperationType);
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

    /// <summary>
    /// 移动文件
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    /// <param name="fileOperationType"></param>
    /// <returns></returns>
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
            if (IsFileLocked(sourcePath))
            {
                HandleFileConflict(sourcePath, targetPath, fileOperationType, () =>
                {
                    ForceProcessFile(sourcePath, targetPath);
                }, true, true);
            }
            // 处理异常（记录日志等）
            LogService.Logger.Error($"Error moving file: {ex.Message}");
        }
        finally
        {
            _semaphore.Release(); // 确保释放互斥锁
        }
    }

    /// <summary>
    /// 移动文件夹
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    /// <param name="fileOperationType"></param>
    /// <returns></returns>
    private static async Task MoveFolder(string sourcePath, string targetPath, FileOperationType fileOperationType)
    {
        await _semaphore.WaitAsync(); // 请求对文件操作的独占访问
        try
        {
            HandleFileConflict(sourcePath, targetPath, fileOperationType, () =>
            {
                Directory.Move(sourcePath, targetPath);
            });
        }
        catch (Exception ex)
        {
            // 处理异常（记录日志等）
            LogService.Logger.Error($"Error moving folder: {ex.Message}");
        }
        finally
        {
            _semaphore.Release(); // 确保释放互斥锁
        }
    }

    /// <summary>
    /// 复制文件
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    /// <param name="fileOperationType"></param>
    /// <returns></returns>
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

    private static async Task CopyFolder(string sourcePath, string targetPath, FileOperationType fileOperationType)
    {
        await _semaphore.WaitAsync(); // 请求对文件操作的独占访问
        try
        {
            HandleFileConflict(sourcePath, targetPath, fileOperationType, () =>
            {
                CopyDirectory(sourcePath, targetPath);
            });
        }
        catch (Exception ex)
        {
            // 处理异常（记录日志等）
            LogService.Logger.Error($"Error copying folder: {ex.Message}");
        }
        finally
        {
            _semaphore.Release(); // 确保释放互斥锁
        }
    }

    /// <summary>
    /// 复制整个文件夹
    /// </summary>
    /// <param name="sourceDir"></param>
    /// <param name="destDir"></param>
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (string filePath in Directory.GetFiles(sourceDir, "*", System.IO.SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceDir, filePath);
            string destFilePath = Path.Combine(destDir, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destFilePath));
            File.Copy(filePath, destFilePath, true);
        }
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
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

    /// <summary>
    /// 删除文件夹
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static async Task DeleteFolder(string path)
    {
        await _semaphore.WaitAsync(); // 请求对文件操作的独占访问
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch (Exception ex)
        {
            // 处理异常（记录日志等）
            LogService.Logger.Error($"Error deleting folder: {ex.Message}");
        }
        finally
        {
            _semaphore.Release(); // 确保释放互斥锁
        }
    }

    /// <summary>
    /// 重命名
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    /// <returns></returns>
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

    private static async Task RenameFolder(string sourcePath, string targetPath)
    {
        await _semaphore.WaitAsync(); // 请求对文件操作的独占访问
        try
        {
            Directory.Move(sourcePath, targetPath);
        }
        catch (Exception ex)
        {
            // 处理异常（记录日志等）
            LogService.Logger.Error($"Error renaming folder: {ex.Message}");
        }
        finally
        {
            _semaphore.Release(); // 确保释放互斥锁
        }
    }

    /// <summary>
    /// 移动文件到回收站
    /// </summary>
    /// <param name="path"></param>
    /// <param name="dynamicFilters"></param>
    /// <param name="pathFilter"></param>
    /// <param name="ruleType"></param>
    /// <param name="deleteSubfolders"></param>
    /// <returns></returns>
    private static async Task MoveToRecycleBin(string path, List<Func<string, bool>> dynamicFilters,
        Func<string, bool>? pathFilter, TaskRuleType ruleType, bool isFolder = false, bool deleteSubfolders = false)
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
                if (isFolder)
                {
                    if (!ShouldSkip(dynamicFilters, path, pathFilter))
                    {
                        FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        return;
                    }
                }
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
                        if (IsFolderEmpty(subfolder))
                        {
                            FileSystem.DeleteDirectory(subfolder, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        }
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

    /// <summary>
    /// 检查文件夹是否为空
    /// </summary>
    /// <param name="folderPath"></param>
    /// <returns></returns>
    private static bool IsFolderEmpty(string folderPath)
    {
        // 检查文件夹路径是否有效
        if (Directory.Exists(folderPath))
        {
            // 获取文件夹中的文件和子文件夹
            var files = Directory.GetFiles(folderPath);
            var directories = Directory.GetDirectories(folderPath);

            // 如果文件夹中没有文件且没有子文件夹，则认为文件夹为空
            return files.Length == 0 && directories.Length == 0;
        }

        // 如果文件夹不存在，返回 false
        return false;
    }

    /// <summary>
    /// 处理文件冲突
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    /// <param name="fileOperationType"></param>
    /// <param name="action"></param>
    /// <param name="isMove"></param>
    /// <param name="inUse"></param>
    /// <exception cref="NotSupportedException"></exception>
    private static void HandleFileConflict(string sourcePath, string targetPath, FileOperationType fileOperationType, Action action, bool isMove = false, bool inUse = false)
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
                    if (File.GetLastWriteTime(sourcePath) > File.GetLastWriteTime(targetPath) 
                        || Directory.GetLastWriteTime(sourcePath) > Directory.GetLastWriteTime(targetPath))
                    {
                        action(); // 执行操作
                    }
                    break;
                case FileOperationType.OverrideIfSizesDiffer:
                    if (AreFileSizesEqual(sourcePath, targetPath))
                    {
                        action(); // 执行操作
                    }
                    break;
                case FileOperationType.ReNameAppend:
                    ProcessPath(sourcePath, targetPath, isMove, inUse, false);
                    break;
                case FileOperationType.ReNameAddDate:
                    ProcessPath(sourcePath, targetPath, isMove, inUse, true);
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

    /// <summary>
    /// 检查是否应该跳过
    /// </summary>
    /// <param name="dynamicFilters"></param>
    /// <param name="path"></param>
    /// <param name="pathFilter"></param>
    /// <returns></returns>
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
        return !satisfiesDynamicFilters && !satisfiesPathFilter;
    }

    /// <summary>
    /// 强制处理文件
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="targetPath"></param>
    private static void ForceProcessFile(string filePath, string targetPath)
    {
        try
        {
            // 获取源文件的文件名
            string sourceFileName = Path.GetFileName(filePath);
            // 构建目标文件的完整路径
            string destinationPath = Path.Combine(targetPath, sourceFileName);
            // 使用 FileShare.ReadWrite 来允许其他进程共享访问文件
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
            {
                // 确保目标路径的目录存在
                string targetDirectory = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);  // 创建目标目录
                }

                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    destinationStream.Write(buffer, 0, bytesRead);
                }
            }
        }
        catch (IOException ex)
        {
            LogService.Logger.Error("文件打开失败：" + ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogService.Logger.Error("文件访问权限不足：" + ex.Message);
        }
    }

    /// <summary>
    /// 检查文件是否被占用
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private static bool IsFileLocked(string filePath)
    {
        try
        {
            // 尝试以独占方式打开文件
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                // 文件成功打开，说明没有被占用
                return false;
            }
        }
        catch (IOException)
        {
            // 文件被占用
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // 无权限访问文件
            return true;
        }
        catch (Exception ex)
        {
            // 其他异常
            LogService.Logger.Error("文件锁定检查失败：" + ex.Message);
            return true;
        }
    }

    /// <summary>
    /// 检查是否是文件
    /// </summary>
    /// <param name="rule"></param>
    /// <param name="ruleType"></param>
    /// <returns></returns>
    private static bool IsFolder(string rule, TaskRuleType ruleType )
    {
        bool isFolder = FilterUtil.ContainsTwoConsecutiveChars(rule, '#');
        bool isAllFolders = FilterUtil.ContainsTwoConsecutiveChars(rule, '*');

        if (ruleType == TaskRuleType.FileRule) 
        {
            return true;
        }
        if (ruleType == TaskRuleType.FolderRule && (isAllFolders || isFolder))
        {
            return true;
        }
        if (ruleType == TaskRuleType.CustomRule && (isAllFolders || isFolder))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取文件夹的大小
    /// </summary>
    /// <param name="folderPath"></param>
    /// <returns></returns>
    /// <exception cref="DirectoryNotFoundException"></exception>
    private static long GetDirectorySize(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Directory not found: {folderPath}");

        return Directory.GetFiles(folderPath, "*", System.IO.SearchOption.AllDirectories)
                        .Sum(file => new FileInfo(file).Length);
    }

    /// <summary>
    /// 检查两个文件夹的大小是否相等
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    /// <returns></returns>
    private static bool AreFileSizesEqual(string sourcePath, string targetPath)
    {
        if (File.Exists(sourcePath) && File.Exists(targetPath))
        {
            // 比较文件大小
            return new FileInfo(sourcePath).Length != new FileInfo(targetPath).Length;
        }
        else if (Directory.Exists(sourcePath) && Directory.Exists(targetPath))
        {
            // 如果是文件夹，调用自定义方法来比较文件夹大小
            return GetDirectorySize(sourcePath) != GetDirectorySize(targetPath);
        }
        else
        {
            LogService.Logger.Error("无效文件夹或者文件路径");
            return false;
        }
    }

    /// <summary>
    /// 获取唯一路径
    /// </summary>
    /// <param name="path"></param>
    /// <param name="withDate"></param>
    /// <returns></returns>
    public static string GetUniquePath(string path, bool withDate = false)
    {
        if (File.Exists(path) || Directory.Exists(path))
        {
            return withDate ? GetUniquePathWithDate(path) : GetUniquePathWithoutDate(path);
        }
        return path;
    }

    // 无时间戳的唯一路径生成器
    private static string GetUniquePathWithoutDate(string path)
    {
        string directory = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        int count = 1;

        string newPath = path;

        if (File.Exists(path)) // 如果是文件
        {
            while (File.Exists(newPath))
            {
                newPath = Path.Combine(directory, $"{name}_{count++}{extension}");
            }
        }
        else if (Directory.Exists(path)) // 如果是文件夹
        {
            while (Directory.Exists(newPath))
            {
                newPath = Path.Combine(directory, $"{name}_{count++}");
            }
        }

        return newPath;
    }

    // 带时间戳的唯一路径生成器
    private static string GetUniquePathWithDate(string path)
    {
        string directory = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");

        string newPath;
        if (File.Exists(path)) // 如果是文件
        {
            newPath = Path.Combine(directory, $"{name}_{timestamp}{extension}");
        }
        else // 如果是文件夹
        {
            newPath = Path.Combine(directory, $"{name}_{timestamp}");
        }

        return newPath;
    }

    /// <summary>
    /// 处理路径
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    /// <param name="isMove"></param>
    /// <param name="inUse"></param>
    /// <param name="withData"></param>
    private static void ProcessPath(string sourcePath, string targetPath, bool isMove, bool inUse, bool withData )
    {
        string newPath = GetUniquePath(targetPath, withData);

        if (!inUse)
        {
            if (File.Exists(sourcePath))
            {
                // 如果是文件，则执行文件的移动或复制
                if (isMove)
                {
                    File.Move(sourcePath, newPath);
                }
                else
                {
                    File.Copy(sourcePath, newPath);
                }
            }
            else if (Directory.Exists(sourcePath))
            {
                // 如果是文件夹，则执行文件夹的移动或复制
                if (isMove)
                {
                    Directory.Move(sourcePath, newPath);
                }
                else
                {
                    CopyDirectory(sourcePath, newPath);
                }
            }
            else
            {
                LogService.Logger.Error("文件夹或者文件未找到 ProcessPath");
            }
        }
        else if (inUse && File.Exists(sourcePath))
        {
            ForceProcessFile(sourcePath, newPath + "_ForceProcessFile");
        }
    }

}
