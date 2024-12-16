using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Util;

namespace EasyTidy.Service;

public class FolderActuator
{
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

    /// <summary>
    /// 执行文件夹操作
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="maxRetries"></param>
    /// <returns></returns>
    public static async Task ExecuteFolderOperationAsync(OperationParameters parameters, int maxRetries = 5)
    {
        try
        {
            if (string.IsNullOrEmpty(parameters.TargetPath)) return;
            if (IsFolder(parameters.RuleModel.Rule, parameters.RuleModel.RuleType) && parameters.OperationMode != OperationMode.Extract)
            {
                await RetryAsync(maxRetries, async () =>
                {
                    await ProcessFoldersAsync(parameters);
                });
            }
            else
            {
                await RetryAsync(maxRetries, async () =>
                {
                    await FileActuator.ExecuteFileOperationAsync(parameters);
                });
            }
        }
        catch (Exception ex)
        {
            LogService.Logger.Error($"Error executing folder operation: {ex.Message}");
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
        if (!Directory.Exists(parameters.TargetPath) && parameters.OperationMode != OperationMode.Rename)
        {
            Directory.CreateDirectory(parameters.TargetPath);
        }

        LogService.Logger.Info($"执行文件夹操作 {parameters.TargetPath}");
        var folderList = Directory.GetDirectories(parameters.SourcePath).ToList();

        if (parameters.OperationMode == OperationMode.ZipFile)
        {
            await ExecuteFolderActionAsync(parameters);
            return;
        }

        foreach (var folder in folderList)
        {
            if (FilterUtil.ShouldSkip(parameters.Funcs, folder, parameters.PathFilter))
            {
                LogService.Logger.Debug($"执行文件夹操作 ShouldSkip {parameters.TargetPath}");
                continue;
            }

            var newParameters = new OperationParameters(
                parameters.OperationMode,
                folder,
                Path.Combine(parameters.TargetPath, Path.GetFileName(folder)),
                parameters.FileOperationType,
                parameters.HandleSubfolders,
                parameters.Funcs,
                parameters.PathFilter)
            { OldTargetPath = parameters.TargetPath, RuleModel = parameters.RuleModel };
            await ExecuteFolderActionAsync(newParameters);
        }
        if (parameters.OperationMode == OperationMode.Rename)
        {
            Renamer.ResetIncrement();
        }
    }

    /// <summary>
    /// 整个文件夹处理
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    private static async Task ExecuteFolderActionAsync(OperationParameters parameters)
    {
        if (FilterUtil.ShouldSkip(parameters.Funcs, parameters.SourcePath, parameters.PathFilter) 
            && parameters.OperationMode != OperationMode.RecycleBin && parameters.OperationMode != OperationMode.ZipFile)
        {
            LogService.Logger.Debug($"执行文件夹操作 {parameters.TargetPath}");
            return;
        }

        LogService.Logger.Info($"执行文件夹操作 {parameters.TargetPath}, 操作模式: {parameters.OperationMode}");
        switch (parameters.OperationMode)
        {
            case OperationMode.Move:
                await MoveFolder(parameters.SourcePath, parameters.TargetPath, parameters.FileOperationType);
                break;
            case OperationMode.Copy:
                await CopyFolder(parameters.SourcePath, parameters.TargetPath, parameters.FileOperationType);
                break;
            case OperationMode.Delete:
                await DeleteFolder(parameters.TargetPath);
                break;
            case OperationMode.Rename:
                await RenameFolder(parameters.SourcePath, parameters.OldTargetPath);
                break;
            case OperationMode.RecycleBin:
                await FileActuator.MoveToRecycleBin(parameters.TargetPath, new List<Func<string, bool>>(parameters.Funcs), 
                    parameters.PathFilter, parameters.RuleModel.RuleType, true, parameters.HandleSubfolders);
                break;
            case OperationMode.UploadWebDAV:
                await UploadFolderAsync(parameters.SourcePath);
                break;
            case OperationMode.ZipFile:
                await CompressFolderAsync(parameters);
                break;
            case OperationMode.Encryption:
                // TODO: 加密文件
                break;
            default:
                throw new NotSupportedException($"Operation mode '{parameters.OperationMode}' is not supported.");
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
            FileResolver.HandleFileConflict(sourcePath, targetPath, fileOperationType, () =>
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

    private static async Task CopyFolder(string sourcePath, string targetPath, FileOperationType fileOperationType)
    {
        await _semaphore.WaitAsync(); // 请求对文件操作的独占访问
        try
        {
            FileResolver.HandleFileConflict(sourcePath, targetPath, fileOperationType, () =>
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

        foreach (string filePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceDir, filePath);
            string destFilePath = Path.Combine(destDir, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destFilePath));
            File.Copy(filePath, destFilePath, true);
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

    private static async Task RenameFolder(string sourcePath, string targetPath)
    {
        await _semaphore.WaitAsync(); // 请求对文件操作的独占访问
        try
        {
            var newPath = Renamer.ParseTemplate(sourcePath, targetPath);
            Directory.Move(sourcePath, newPath);
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

    private static bool IsFolder(string rule, TaskRuleType ruleType)
    {
        bool isFolder = FilterUtil.ContainsTwoConsecutiveChars(rule, '#');
        bool isAllFolders = FilterUtil.ContainsTwoConsecutiveChars(rule, '*');

        if (ruleType == TaskRuleType.FileRule)
        {
            return false;
        }
        if (ruleType == TaskRuleType.FolderRule)
        {
            return true;
        }
        if (ruleType == TaskRuleType.CustomRule && (isAllFolders || isFolder))
        {
            return true;
        }
        return false;
    }

    private static async Task UploadFolderAsync(string localDirPath, string remoteDirPath = null)
    {
        var password = CryptoUtil.DesDecrypt(ServiceConfig.CurConfig.WebDavPassword);
        WebDavClient webDavClient = new(ServiceConfig.CurConfig.WebDavUrl, ServiceConfig.CurConfig.WebDavUser, password);
        remoteDirPath = Path.Combine(remoteDirPath ?? ServiceConfig.CurConfig.WebDavUrl + ServiceConfig.CurConfig.UploadPrefix, Path.GetFileName(localDirPath));
        await webDavClient.CreateFolderAsync(ServiceConfig.CurConfig.WebDavUrl + ServiceConfig.CurConfig.UploadPrefix, Path.GetFileName(localDirPath));
        foreach (var file in Directory.GetFiles(localDirPath))
        {
            string remoteFilePath = Path.Combine(remoteDirPath, Path.GetFileName(file));
            await webDavClient.UploadFileAsync(remoteFilePath, file);
        }
        // 递归上传子目录
        foreach (string subDir in Directory.GetDirectories(localDirPath))
        {
            string remoteSubDirPath = Path.Combine(remoteDirPath, Path.GetFileName(subDir));
            await UploadFolderAsync(subDir, remoteSubDirPath);
        }
    }

    private static async Task CompressFolderAsync(OperationParameters parameters)
    {
        LogService.Logger.Info($"处理文件夹压缩操作: {parameters.SourcePath}");

        var directoriesToCompress = new List<string>();

        // 收集符合条件的文件夹
        await CollectDirectoriesForCompression(parameters, directoriesToCompress);

        if (directoriesToCompress.Any())
        {
            var zipFilePath = Path.Combine(parameters.TargetPath, $"{Path.GetFileName(parameters.TargetPath)}.zip");
            // 调用压缩文件夹的方法
            CreateCompressedArchiveForFolders(parameters.SourcePath, zipFilePath, directoriesToCompress);
            LogService.Logger.Info($"文件夹压缩完成，生成压缩包: {parameters.TargetPath}");
        }
        else
        {
            LogService.Logger.Warn($"未找到符合条件的文件夹，跳过压缩操作: {parameters.SourcePath}");
        }
    }

    private static async Task CollectDirectoriesForCompression(OperationParameters parameters, List<string> directoriesToCompress)
    {
        // 获取当前目录下的所有文件夹
        var dirList = Directory.GetDirectories(parameters.SourcePath).ToList();

        foreach (var dirPath in dirList)
        {
            // 使用过滤条件判断是否跳过当前文件夹
            if (FilterUtil.ShouldSkip(parameters.Funcs, dirPath, parameters.PathFilter))
            {
                LogService.Logger.Debug($"跳过文件夹: {dirPath}");
                continue;
            }

            // 如果符合条件，添加到待处理文件夹列表
            directoriesToCompress.Add(dirPath);

            // 递归处理子文件夹
            var subDirParameters = new OperationParameters(
                parameters.OperationMode,
                dirPath,
                parameters.TargetPath,
                parameters.FileOperationType,
                parameters.HandleSubfolders,
                parameters.Funcs,
                parameters.PathFilter)
            {
                OldTargetPath = parameters.TargetPath,
                OldSourcePath = dirPath,
                RuleModel = parameters.RuleModel
            };

            await CollectDirectoriesForCompression(subDirParameters, directoriesToCompress);
        }
    }

    private static void CreateCompressedArchiveForFolders(string sourcePath, string targetPath, List<string> directoriesToCompress)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetFileName(sourcePath));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            // 遍历待压缩的文件夹
            foreach (var directoryPath in directoriesToCompress)
            {
                // 计算相对路径以保持层级结构
                string relativeDirPath = Path.GetRelativePath(sourcePath, directoryPath);
                string destinationDirPath = Path.Combine(tempDirectory, relativeDirPath);

                Directory.CreateDirectory(destinationDirPath);

                // 复制文件夹内的文件
                CopyDirectory(directoryPath, destinationDirPath);
            }

            // 创建压缩包
            ZipFile.CreateFromDirectory(tempDirectory, targetPath, CompressionLevel.Fastest, true);
        }
        catch (Exception ex)
        {
            LogService.Logger.Error($"压缩文件夹时发生错误: {ex.Message}", ex);
            throw;
        }
        finally
        {
            // 清理临时目录
            Directory.Delete(tempDirectory, true);
        }
    }

}
