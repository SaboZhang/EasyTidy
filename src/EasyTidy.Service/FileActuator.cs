using CommunityToolkit.WinUI;
using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Util;
using Microsoft.VisualBasic.FileIO;
using Org.BouncyCastle.Utilities.Encoders;
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
        // 创建目标目录（如果需要）
        EnsureTargetDirectory(parameters);

        // 根据操作模式执行相应的处理
        switch (parameters.OperationMode)
        {
            case OperationMode.ZipFile:
                await ProcessFileAsync(parameters);
                break;
            case OperationMode.Rename:
                await HandleRenameOperation(parameters);
                break;
            default:
                await HandleDefaultOperation(parameters);
                break;
        }
    }

    private static void EnsureTargetDirectory(OperationParameters parameters, string? directoryPath = null)
    {
        if (!Directory.Exists(parameters.TargetPath) 
        && parameters.OperationMode != OperationMode.Rename && string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(parameters.TargetPath);
        }
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    private static async Task HandleDefaultOperation(OperationParameters parameters)
    {
        // 递归处理子文件夹（如果启用）
        if (CommonUtil.Configs?.GeneralConfig?.SubFolder ?? false)
        {
            await ProcessSubDirectoriesAsync(parameters);
        }

        var fileList = Directory.GetFiles(parameters.SourcePath).ToList();
        foreach (var filePath in fileList)
        {
            if (ShouldSkipFile(filePath, parameters))
            {
                LogService.Logger.Debug($"执行文件操作，获取所有文件跳过不符合条件的文件 filePath: {filePath}, RuleFilters: {parameters.RuleName}");
                continue;
            }

            var targetFilePath = Path.Combine(parameters.TargetPath, Path.GetFileName(filePath));
            var fileParameters = CreateOperationParametersForFile(parameters, targetFilePath, filePath);
            await ProcessFileAsync(fileParameters);
        }
    }

    private static bool ShouldSkipFile(string filePath, OperationParameters parameters)
    {
        return FilterUtil.ShouldSkip(new List<Func<string, bool>>(parameters.Funcs), filePath, parameters.PathFilter);
    }

    private static OperationParameters CreateOperationParametersForFile(
        OperationParameters baseParameters,
        string targetFilePath,
        string sourceFilePath)
    {
        return new OperationParameters(
            baseParameters.OperationMode,
            sourceFilePath,
            targetFilePath,
            baseParameters.FileOperationType,
            baseParameters.HandleSubfolders,
            baseParameters.Funcs,
            baseParameters.PathFilter)
        {
            OldTargetPath = baseParameters.TargetPath,
            OldSourcePath = sourceFilePath,
            RuleModel = baseParameters.RuleModel,
            AIServiceLlm = baseParameters.AIServiceLlm
        };
    }

    private static async Task HandleRenameOperation(OperationParameters parameters)
    {
        // 处理重命名操作
        await HandleDefaultOperation(parameters);
        Renamer.ResetIncrement();
    }

    private static async Task ProcessSubDirectoriesAsync(OperationParameters parameters)
    {
        var subDirList = Directory.GetDirectories(parameters.SourcePath).ToList();
        var files = GetAllFiles(subDirList);
        foreach (var file in files)
        {
            if (IsHardLinkedFile(file))
            {
                continue;
            }
            string destinationPath;
            if (CommonUtil.Configs?.PreserveDirectoryStructure ?? true)
            {
                // 保留目录结构时，获取相对路径
                string relativePath = Path.GetRelativePath(parameters.SourcePath, file);
                destinationPath = Path.Combine(parameters.TargetPath, relativePath);
            }
            else
            {
                // 如果不保留目录结构，直接处理到 target 目录
                destinationPath = Path.Combine(parameters.TargetPath, Path.GetFileName(file));
            }
            string destinationDir = Path.GetDirectoryName(destinationPath);

            EnsureTargetDirectory(parameters, destinationDir); // 确保每个子目录的目标路径存在

            var fileParameters = CreateOperationParametersForFile(parameters, destinationPath, file);
            await ProcessFileAsync(fileParameters);
        }
    }

    private static List<string> GetAllFiles(List<string> listPaths)
    {
        List<string> files = new();
        try
        {
            foreach (var path in listPaths)
            {
                files.AddRange(Directory.EnumerateFiles(path, "*.*", System.IO.SearchOption.AllDirectories));
            }
        }
        catch (Exception ex)
        {
            LogService.Logger.Error($"Error getting all files error: {ex.Message}");
        }

        return files;
    }

    /// <summary>
    /// 递归获取所有文件 预留方法，若效率太低则使用此方法进行递归
    /// </summary>
    /// <param name="path"></param>
    /// <param name="folderName"></param>
    /// <param name="fileName"></param>
    /// <param name="projectPaths"></param>
    private static void SearchFileInPath(string path, string folderName, string fileName, ref List<string> projectPaths)
    {
        var dirs = Directory.GetDirectories(path, "*.*", System.IO.SearchOption.TopDirectoryOnly).ToList();  //获取当前路径下所有文件与文件夹
        var desFolders = dirs.FindAll(x => x.Contains(folderName)); //在当前目录中查找目标文件夹
        if (desFolders == null || desFolders.Count <= 0)
        {
            //当前目录未找到目标则递归
            foreach (var dir in dirs)
            {
                SearchFileInPath(dir, folderName, fileName, ref projectPaths);
            }
        }
        else
        {
            //找到则添加至结果集
            projectPaths.Add(path + "\\" + folderName + "\\" + fileName);
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
        if (!CommonUtil.Configs?.GeneralConfig.EmptyFiles ?? true) 
        {
             FileInfo info = new(parameters.SourcePath);
             if (info.Length == 0)
             {
                 LogService.Logger.Debug($"执行文件操作 跳过空文件 {parameters.TargetPath}");
                 return;
             }
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
                var password = CryptoUtil.DesDecrypt(CommonUtil.Configs.WebDavPassword);
                WebDavClient webDavClient = new(CommonUtil.Configs.WebDavUrl, CommonUtil.Configs.WebDavUser, password);
                await webDavClient.UploadFileAsync(CommonUtil.Configs.WebDavUrl + CommonUtil.Configs.UploadPrefix, parameters.SourcePath);
                break;
            case OperationMode.ZipFile:
                await CompressFileAsync(parameters);
                break;
            case OperationMode.Encryption:
                var pass = CryptoUtil.DesDecrypt(CommonUtil.Configs.EncryptedPassword);
                await ExecuteEncryption(parameters.SourcePath, parameters.TargetPath, pass);
                break;
            case OperationMode.HardLink:
                CreateFileHardLink(parameters.SourcePath, parameters.TargetPath);
                break;
            case OperationMode.SoftLink:
                CreateFileSymbolicLink(parameters.SourcePath, parameters.TargetPath);
                break;
            case OperationMode.AISummary:
                await CreateAISummary(parameters);
                break;
            case OperationMode.AIClassification:
                await CreateAIClassification(parameters);
                break;
            default:
                throw new NotSupportedException($"Operation mode '{parameters.OperationMode}' is not supported.");
        }
    }

    private static async Task CreateAIClassification(OperationParameters parameters)
    {
        var cts = new CancellationTokenSource();
        await StreamHandlerAsync(parameters.AIServiceLlm,parameters.Prompt, cts.Token);
        // 取消任务
        // cts.Cancel();
    }

    private static async Task CreateAISummary(OperationParameters parameters)
    {
        var cts = new CancellationTokenSource();
        parameters.Prompt = "总结";
        var fileType = FileReader.GetFileType(parameters.SourcePath);
        string conntent = string.Empty;
        switch (fileType)
        {
            case FileType.Xls:
            case FileType.Xlsx:
                var conntents = FileReader.ReadExcel(parameters.SourcePath);
                foreach (var item in conntents)
                {
                    conntent += item;
                }
                break;
            case FileType.Doc:
            case FileType.Docx:
                conntent = FileReader.ReadWord(parameters.SourcePath);
                break;
            case FileType.Pdf:
                conntent = FileReader.ExtractTextFromPdf(parameters.SourcePath);
                break;
            default:
                LogService.Logger.Error($"不支持的文件类型: {fileType}");
                cts.Cancel();
                return;
        }
        await StreamHandlerAsync(parameters.AIServiceLlm, conntent, cts.Token);
        string formattedTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        string fileName = $"{"AISummaryText".GetLocalized()}-{formattedTime}.pdf";
        string filePath = Path.Combine(parameters.OldTargetPath, fileName);
        if (parameters.AIServiceLlm.Data.IsSuccess)
        {
            FileWriterUtil.WriteObjectToPdf(parameters.AIServiceLlm.Data.Result, filePath);
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

        if (CommonUtil.Configs.GeneralConfig?.SubFolder ?? false)
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

    private static void CreateFileSymbolicLink(string filePath, string tragetPath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            LogService.Logger.Warn($"无效路径: {filePath}");
            return;
        }
        try
        {
            // 如果符号链接已存在且指向相同目标，跳过创建
            if (IsSameSymbolicLink(filePath, tragetPath))
            {
                LogService.Logger.Warn($"Symbolic link already exists: {filePath} -> {tragetPath}");
                return;
            }
            // 如果路径已存在但不是符号链接，则抛出异常
            if (File.Exists(filePath) || Directory.Exists(filePath))
            {
                throw new IOException($"A file or directory already exists at the path: {filePath}");
            }
            var result = File.CreateSymbolicLink(filePath, tragetPath) as FileInfo;
            LogService.Logger.Info($"创建符号连接成功: {filePath} -> {tragetPath}");
        }
        catch (Exception ex)
        {
            LogService.Logger.Error($"Error creating symbolic link: {ex.Message}");
        }
    }

    private static bool IsSymbolicLink(string path)
    {
        FileSystemInfo fileInfo = new FileInfo(path);

        // 如果路径是目录，则使用 DirectoryInfo
        if (Directory.Exists(path))
        {
            fileInfo = new DirectoryInfo(path);
        }

        return fileInfo.LinkTarget != null;
    }

    private static bool IsSameSymbolicLink(string linkPath, string targetPath)
    {
        if (!IsSymbolicLink(linkPath))
        {
            return false;
        }

        FileSystemInfo fileInfo = new FileInfo(linkPath);

        // 如果路径是目录，则使用 DirectoryInfo
        if (Directory.Exists(linkPath))
        {
            fileInfo = new DirectoryInfo(linkPath);
        }

        // 比较目标路径
        return string.Equals(fileInfo.LinkTarget, targetPath, StringComparison.OrdinalIgnoreCase);
    }

    private static void CreateFileHardLink(string source, string target)
    {
        try
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            {
                LogService.Logger.Warn($"无效路径: Source: {source}, Target: {target}");
                return;
            }

            source = Path.GetFullPath(source);
            target = Path.GetFullPath(target);

            // 检查源文件是否存在
            if (!File.Exists(source))
            {
                LogService.Logger.Warn($"源文件不存在: {source}");
                return;
            }

            // 检查目标路径是否存在
            if (File.Exists(target))
            {
                // 检查目标是否为指向源的硬链接
                if (IsHardLink(source, target))
                {
                    LogService.Logger.Info($"硬链接已存在: {source} -> {target}");
                    return;
                }

                LogService.Logger.Warn($"目标路径已存在，但不是硬链接: {target}");
                return;
            }

            // 尝试创建硬链接
            bool result = CreateHardLink(target, source);
            if (result)
            {
                LogService.Logger.Info($"创建硬链接成功: {source} -> {target}");
            }
            else
            {
                LogService.Logger.Warn($"创建硬链接失败: {source} -> {target}");
            }
        }
        catch (Exception ex)
        {
            LogService.Logger.Error($"创建硬链接失败: {ex}");
        }
    }


    private static bool IsHardLink(string source, string target)
    {
        try
        {
            var sourceInfo = new FileInfo(source);
            var targetInfo = new FileInfo(target);

            // 比较源文件和目标文件的文件标识符（如 NTFS 下的文件索引号）
            return sourceInfo.Exists && targetInfo.Exists &&
                   sourceInfo.Length == targetInfo.Length &&
                   sourceInfo.LastWriteTimeUtc == targetInfo.LastWriteTimeUtc;
        }
        catch (Exception ex)
        {
            LogService.Logger.Error($"检测硬链接失败: {ex}");
            return false;
        }
    }

    /// <summary>
    /// 创建硬链接
    /// </summary>
    /// <param name="hardLinkPath">新硬链接的路径</param>
    /// <param name="existingFilePath">现有文件的路径</param>
    /// <returns>是否成功</returns>
    private static bool CreateHardLink(string hardLinkPath, string existingFilePath)
    {
        if (string.IsNullOrWhiteSpace(hardLinkPath))
            LogService.Logger.Error($"Hard link path cannot be null or empty.{nameof(hardLinkPath)}");

        if (string.IsNullOrWhiteSpace(existingFilePath))
            LogService.Logger.Error($"Existing file path cannot be null or empty.{nameof(existingFilePath)}");

        bool result = CreateHardLink(hardLinkPath, existingFilePath, IntPtr.Zero);
        if (!result)
        {
            int errorCode = Marshal.GetLastWin32Error();
            throw new System.ComponentModel.Win32Exception(errorCode, "Failed to create hard link.");
        }
        return result;
    }

    private static async Task ExecuteEncryption(string path, string target, string pass)
    {
        var enc = CommonUtil.Configs.Encrypted;
        switch (enc)
        {
            case Encrypted.SevenZip:
                ZipUtil.CompressWithPassword(path, target, pass);
                break;
            case Encrypted.AES256WithPBKDF2DerivedKey:
                CryptoUtil.EncryptFile(path, target, pass);
                break;
        }
        await Task.CompletedTask;
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

    // P/Invoke 声明 CreateHardLink
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool CreateHardLink(
        string lpFileName,  // 新硬链接的路径
        string lpExistingFileName,  // 现有文件的路径
        IntPtr lpSecurityAttributes);  // 保留，必须为 NULL

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(IntPtr hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

    [StructLayout(LayoutKind.Sequential)]
    private struct BY_HANDLE_FILE_INFORMATION
    {
        public uint FileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks; // 硬链接计数
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    public static bool IsHardLinkedFile(string filePath)
    {
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            if (GetFileInformationByHandle(fs.SafeFileHandle.DangerousGetHandle(), out BY_HANDLE_FILE_INFORMATION fileInfo))
            {
                return fileInfo.NumberOfLinks > 1;
            }
            else
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }

    private static async Task StreamHandlerAsync(IAIServiceLlm aIServiceLlm, string content, CancellationToken token)
    {
        aIServiceLlm.Data = ServiceResult.Reset;

        await aIServiceLlm.PredictAsync(
            new RequestModel(content),
            msg =>
            {
                LogService.Logger.Info($"AI 服务返回: {msg}");
                aIServiceLlm.Data.IsSuccess = true;
                aIServiceLlm.Data.Result += msg;
            }, 
            token
        );
    }

    private static async Task NonStreamHandlerAsync(IAIServiceLlm aIServiceLlm, string content, CancellationToken token)
    {
        aIServiceLlm.Data = await aIServiceLlm.PredictAsync(new RequestModel(content), token);
    }

}
