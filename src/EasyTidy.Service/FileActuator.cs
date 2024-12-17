using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Util;
using Microsoft.VisualBasic.FileIO;
using SharpCompress.Common;
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
            LogService.Logger.Debug("Removed operation lock from executed operations.");
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
        if (!Directory.Exists(parameters.TargetPath) && parameters.OperationMode != OperationMode.Rename)
        {
            Directory.CreateDirectory(parameters.TargetPath);
        }

        if (parameters.OperationMode == OperationMode.ZipFile) 
        {
            await ProcessFileAsync(parameters);
            return;
        }

        var fileList = Directory.GetFiles(parameters.SourcePath).ToList();
        // 获取所有文件并处理
        int fileCount = 0;
        foreach (var filePath in fileList)
        {
            fileCount++;
            var dynamicFilters = new List<Func<string, bool>>(parameters.Funcs);
            if (FilterUtil.ShouldSkip(new List<Func<string, bool>>(parameters.Funcs), filePath, parameters.PathFilter))
            {
                LogService.Logger.Debug($"执行文件操作，获取所有文件跳过不符合条件的文件 filePath: {filePath}, RuleFilters: {parameters.RuleName},=== {fileCount}, sourcePath: {parameters.SourcePath}");
                continue; // 跳过不符合条件的文件
            }

            // 更新目标路径
            var targetFilePath = Path.Combine(parameters.TargetPath, Path.GetFileName(filePath));
            var oldPath = Path.Combine(parameters.SourcePath, Path.GetFileName(filePath));
            var fileParameters = new OperationParameters(
                parameters.OperationMode,
                filePath,
                targetFilePath,
                parameters.FileOperationType,
                parameters.HandleSubfolders,
                parameters.Funcs,
                parameters.PathFilter )
            {
                OldTargetPath = parameters.TargetPath,
                OldSourcePath = oldPath,
                RuleModel = parameters.RuleModel
            };

            await ProcessFileAsync(fileParameters);
        }

        if (parameters.OperationMode == OperationMode.Rename)
        {
            Renamer.ResetIncrement();
        }

        var subFolder = ServiceConfig.CurConfig?.SubFolder ?? false;
        // 递归处理子文件夹
        if (subFolder)
        {
            var subDirList = Directory.GetDirectories(parameters.SourcePath).ToList();
            foreach (var subDir in subDirList)
            {
                if (subDir.Equals(parameters.TargetPath, StringComparison.OrdinalIgnoreCase))
                {
                    LogService.Logger.Debug($"跳过递归目标目录 {subDir}");
                    continue;
                }
                // 为子文件夹生成新的目标路径
                var newTargetPath = Path.Combine(parameters.TargetPath, Path.GetFileName(subDir));
                var oldPath = Path.Combine(parameters.SourcePath, Path.GetFileName(subDir));
                LogService.Logger.Info($"执行文件操作，递归处理子文件夹: {newTargetPath}");

                // 递归调用，传递新的目标路径
                await ProcessDirectoryAsync(new OperationParameters(
                    parameters.OperationMode,
                    subDir,
                    newTargetPath,
                    parameters.FileOperationType,
                    parameters.HandleSubfolders,
                    parameters.Funcs,
                    parameters.PathFilter)
                {
                    OldTargetPath = parameters.TargetPath,
                    OldSourcePath = oldPath,
                    RuleModel = parameters.RuleModel
                });
            }

            if (parameters.OperationMode == OperationMode.Rename)
            {
                Renamer.ResetIncrement();
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
        if (FilterUtil.ShouldSkip(parameters.Funcs, parameters.SourcePath, parameters.PathFilter) 
            && parameters.OperationMode != OperationMode.RecycleBin && parameters.OperationMode != OperationMode.ZipFile)
        {
            LogService.Logger.Debug($"执行文件操作 ShouldSkip {parameters.TargetPath}");
            return;
        }

        LogService.Logger.Info($"开始执行文件操作{parameters.TargetPath}, 操作模式: {parameters.OperationMode}");
        switch (parameters.OperationMode)
        {
            case OperationMode.Move:
                await MoveFile(parameters.SourcePath, parameters.TargetPath, parameters.FileOperationType);
                break;
            case OperationMode.Copy:
                await CopyFile(parameters.SourcePath, parameters.TargetPath, parameters.FileOperationType);
                break;
            case OperationMode.Delete:
                await DeleteFile(parameters.TargetPath);
                break;
            case OperationMode.Rename:
                await RenameFile(parameters.OldSourcePath, parameters.OldTargetPath);
                break;
            case OperationMode.RecycleBin:
                await MoveToRecycleBin(parameters.TargetPath, new List<Func<string, bool>>(parameters.Funcs),
                    parameters.PathFilter, parameters.RuleModel.RuleType, parameters.HandleSubfolders);
                break;
            case OperationMode.Extract:
                Extract(parameters.SourcePath, parameters.TargetPath, parameters.RuleModel.Rule);
                break;
            case OperationMode.UploadWebDAV:
                var password = CryptoUtil.DesDecrypt(ServiceConfig.CurConfig.WebDavPassword);
                WebDavClient webDavClient = new(ServiceConfig.CurConfig.WebDavUrl, ServiceConfig.CurConfig.WebDavUser, password);
                await webDavClient.UploadFileAsync(ServiceConfig.CurConfig.WebDavUrl + ServiceConfig.CurConfig.UploadPrefix, parameters.SourcePath);
                break;
            case OperationMode.ZipFile:
                await CompressFileAsync(parameters);
                break;
            case OperationMode.Encryption:
                // TODO: 加密文件
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
            FileResolver.HandleFileConflict(sourcePath, targetPath, fileOperationType, () =>
            {
                File.Move(sourcePath, targetPath, true);
            });
        }
        catch (Exception ex)
        {
            if (IsFileLocked(sourcePath))
            {
                FileResolver.HandleFileConflict(sourcePath, targetPath, fileOperationType, () =>
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
            FileResolver.HandleFileConflict(sourcePath, targetPath, fileOperationType, () =>
            {
                File.Copy(sourcePath, targetPath, true);
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
            var newPath = Renamer.ParseTemplate(sourcePath, targetPath);
            File.Move(sourcePath, newPath);

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

    /// <summary>
    /// 移动文件到回收站
    /// </summary>
    /// <param name="path"></param>
    /// <param name="dynamicFilters"></param>
    /// <param name="pathFilter"></param>
    /// <param name="ruleType"></param>
    /// <param name="deleteSubfolders"></param>
    /// <returns></returns>
    internal static async Task MoveToRecycleBin(string path, List<Func<string, bool>> dynamicFilters,
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
                    if (!FilterUtil.ShouldSkip(dynamicFilters, path, pathFilter))
                    {
                        FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        return;
                    }
                }
                if (deleteSubfolders)
                {
                    foreach (var subfolder in Directory.GetDirectories(path))
                    {
                        if (FilterUtil.ShouldSkip(dynamicFilters, path, pathFilter) && ruleType != TaskRuleType.FileRule)
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
                    if (FilterUtil.ShouldSkip(dynamicFilters, path, pathFilter) && ruleType != TaskRuleType.FolderRule)
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

    private static async Task CompressFileAsync(OperationParameters parameters)
    {
        LogService.Logger.Info($"处理文件夹压缩操作: {parameters.SourcePath}");

        var filesToCompress = new List<string>();

        // 收集符合条件的文件
        await CollectFilesForCompression(parameters, filesToCompress);

        if (filesToCompress.Any())
        {
            var zipFilePath = Path.Combine(parameters.TargetPath, $"{Path.GetFileName(parameters.TargetPath)}.zip");
            // 调用压缩文件方法
            ZipUtil.CompressFile(parameters.SourcePath, zipFilePath, filesToCompress);
            LogService.Logger.Info($"压缩完成，生成压缩包: {zipFilePath}");
        }
        else
        {
            LogService.Logger.Warn($"未找到符合条件的文件，跳过压缩操作: {parameters.SourcePath}");
        }
    }

    private static async Task CollectFilesForCompression(OperationParameters parameters, List<string> filesToCompress)
    {
        var fileList = Directory.GetFiles(parameters.SourcePath).ToList();

        foreach (var filePath in fileList)
        {
            if (FilterUtil.ShouldSkip(parameters.Funcs, filePath, parameters.PathFilter))
            {
                LogService.Logger.Debug($"跳过文件: {filePath}");
                continue;
            }

            filesToCompress.Add(filePath);
        }

        if (ServiceConfig.CurConfig?.SubFolder ?? false)
        {
            var subDirList = Directory.GetDirectories(parameters.SourcePath).ToList();
            foreach (var subDir in subDirList)
            {
                if (subDir.Equals(parameters.TargetPath, StringComparison.OrdinalIgnoreCase))
                {
                    LogService.Logger.Debug($"跳过递归目标目录 {subDir}");
                    continue;
                }

                var subDirParameters = new OperationParameters(
                    parameters.OperationMode,
                    subDir,
                    parameters.TargetPath,
                    parameters.FileOperationType,
                    parameters.HandleSubfolders,
                    parameters.Funcs,
                    parameters.PathFilter)
                {
                    OldTargetPath = parameters.TargetPath,
                    OldSourcePath = subDir,
                    RuleModel = parameters.RuleModel
                };

                await CollectFilesForCompression(subDirParameters, filesToCompress);
            }
        }
    }

    /// <summary>
    /// 根据文件类型执行解压或提取。
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="filterExtension">指定提取的文件扩展名（为空表示提取所有文件）</param>
    public static void Extract(string filePath, string tragetPath, string filterExtension = null)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            LogService.Logger.Warn($"无效路径: {filePath}");
            return;
        }

        if (string.IsNullOrEmpty(filterExtension))
        {
            filterExtension = GetPartAfterAtSymbolWithoutQuotes(filterExtension);
        }

        string extension = Path.GetExtension(filePath).ToLower();

        // 判断是否为压缩包
        if (FileResolver.IsArchiveFile(extension))
        {
            ExtractArchive(filePath, tragetPath, filterExtension);
        }
        else
        {
            LogService.Logger.Warn($"当前不支持处理 {extension} 文件，路径: {filePath}");
        }
    }

    private static string GetPartAfterAtSymbolWithoutQuotes(string input)
    {
        var parts = input.Split(new[] { "@" }, StringSplitOptions.None);

        return parts.Length > 1 ? parts[1].Trim(';').Replace("\"", "") : string.Empty;
    }

    /// <summary>
    /// 提取压缩文件的主逻辑。
    /// </summary>
    /// <param name="zipFilePath">压缩文件路径</param>
    public static void ExtractArchive(string zipFilePath, string tragetPath, string filterExtension = null)
    {
        if (string.IsNullOrWhiteSpace(zipFilePath) || !File.Exists(zipFilePath))
        {
            LogService.Logger.Warn($"无效路径: {zipFilePath}");
            return;
        }

        // 获取文件的目录和名称
        string directoryPath = Path.GetDirectoryName(zipFilePath);
        if (!tragetPath.Equals(zipFilePath))
        {
            directoryPath = tragetPath;
        }
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(zipFilePath);

        // 解析压缩包内容
        var (isSingleFile, isSingleDirectory, rootFolderName) = FileResolver.AnalyzeCompressedContent(zipFilePath);

        // 根据解析结果选择提取方式
        try
        {
            if (isSingleFile)
            {
                ZipUtil.ExtractSingleFile(zipFilePath, directoryPath, filterExtension);
            }
            else if (isSingleDirectory && rootFolderName != null)
            {
                ZipUtil.ExtractSingleDirectory(zipFilePath, directoryPath, rootFolderName, filterExtension);
            }
            else
            {
                ZipUtil.ExtractToNamedFolder(zipFilePath, directoryPath, fileNameWithoutExtension, filterExtension);
            }
        }
        catch (Exception ex)
        {
            LogService.Logger.Error($"Error during extract process: {ex.Message}");
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
    /// 强制处理文件
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="targetPath"></param>
    internal static void ForceProcessFile(string filePath, string targetPath)
    {
        try
        {
            LogService.Logger.Warn($"开始强制处理文件: {filePath}");
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

}
