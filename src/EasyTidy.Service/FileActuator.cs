using CommunityToolkit.WinUI;
using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Util;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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
                || (string.IsNullOrEmpty(parameters.TargetPath)
                && parameters.OperationMode != OperationMode.AIClassification
                && parameters.OperationMode != OperationMode.RunExternalPrograms))
            {
                return;
            }

            maxRetries = parameters.OperationMode switch
            {
                OperationMode.AIClassification or OperationMode.AISummary => 1,
                _ => maxRetries
            };

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
                else if (parameters.OperationMode == OperationMode.RunExternalPrograms)
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
            case OperationMode.AIClassification:
                await CreateAIClassification(parameters);
                break;
            default:
                await HandleDefaultOperation(parameters);
                break;
        }
    }

    private static void EnsureTargetDirectory(OperationParameters parameters, string? directoryPath = null)
    {
        if (!Directory.Exists(parameters.TargetPath)
            && parameters.OperationMode != OperationMode.Rename
            && parameters.OperationMode != OperationMode.AIClassification
            && parameters.OperationMode != OperationMode.Delete
            && parameters.OperationMode != OperationMode.RunExternalPrograms
            && parameters.OperationMode != OperationMode.RecycleBin
            && string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(parameters.TargetPath);
        }
        if (!string.IsNullOrEmpty(directoryPath)
        && parameters.OperationMode != OperationMode.Delete
        && parameters.OperationMode != OperationMode.RecycleBin
        && !Directory.Exists(directoryPath))
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
        return FilterUtil.ShouldSkip(new List<FilterItem>(parameters.Funcs), filePath, parameters.PathFilter);
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
            AIServiceLlm = baseParameters.AIServiceLlm,
            Prompt = baseParameters.Prompt
        };
    }

    private static async Task HandleRenameOperation(OperationParameters parameters)
    {
        if (!string.IsNullOrEmpty(parameters.RenamePattern) && !"UNSUPPORTED".Equals(parameters.RenamePattern))
        {
            parameters.TargetPath += parameters.RenamePattern;
        }
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
            && parameters.OperationMode != OperationMode.RecycleBin
            && parameters.OperationMode != OperationMode.ZipFile
            && parameters.OperationMode != OperationMode.RunExternalPrograms)
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
                await RenameFile(parameters.OldSourcePath, parameters.OldTargetPath, parameters);
                break;
            case OperationMode.RecycleBin:
                await MoveToRecycleBin(parameters.TargetPath, new List<FilterItem>(parameters.Funcs),
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
            case OperationMode.RunExternalPrograms:
                await ExecuteExternal(parameters.SourcePath, parameters.Argument, parameters.TargetPath);
                break;
            default:
                throw new NotSupportedException($"Operation mode '{parameters.OperationMode}' is not supported.");
        }
    }

    /// <summary>
    /// 初始化AI分类所需的提示配置
    /// </summary>
    private static List<Prompt> InitializePrompts(OperationParameters parameters)
    {
        var systemPrompt = new Prompt("system", PromptConstants.SystemPrompt);
        var userPrompt = new Prompt("user", PromptConstants.UserPrompt);
        var prompts = new List<Prompt> { systemPrompt, userPrompt };

        var userDefinePrompt = new UserDefinePrompt("分类", prompts, true);
        var userDefinePrompts = new List<UserDefinePrompt> { userDefinePrompt };
        parameters.AIServiceLlm.UserDefinePrompts = userDefinePrompts;

        return prompts;
    }

    /// <summary>
    /// 执行AI分类的步骤1：判断操作类型
    /// </summary>
    private static async Task<Dictionary<string, string>> ExecuteStepOne(IAIServiceLlm aiService, string customPrompt, string lang, CancellationToken token)
    {
        await StreamHandlerAsync(aiService, customPrompt, lang, token);
        var setup1 = aiService.Data.Result?.ToString() ?? string.Empty;
        LogService.Logger.Info($"步骤1 - 判断操作类型: {setup1}");
        return FilterUtil.ExtractKeyValuePairsFromRaw(setup1);
    }

    /// <summary>
    /// 执行AI分类的步骤4：获取过滤器配置
    /// </summary>
    private static async Task<FilterTable> ExecuteStepFour(
        IAIServiceLlm aiService,
        List<Prompt> prompts,
        string filter,
        string lang,
        CancellationToken token)
    {
        var systemPromptToUpdate = prompts.FirstOrDefault(p => p.Role.Equals("system"));
        if (systemPromptToUpdate == null)
        {
            LogService.Logger.Warn("未找到用户提示配置，返回空的过滤器配置");
            return new FilterTable();
        }

        systemPromptToUpdate.Content = PromptConstants.SetupFourPrompt;
        var userPromptToUpdate = prompts.FirstOrDefault(p => p.Role.Equals("user"));
        userPromptToUpdate.Content = "Here are my requirements: \r\n $source";
        await StreamHandlerAsync(aiService, filter, lang, token);
        var setup4 = aiService.Data.Result?.ToString() ?? string.Empty;
        LogService.Logger.Debug($"步骤4 - 获取过滤器配置: {setup4}");

        if (string.IsNullOrEmpty(setup4))
        {
            LogService.Logger.Warn("步骤4返回结果为空，返回空的过滤器配置");
            return new FilterTable();
        }

        try
        {
            return FilterUtil.ParseFilterTableFromJson(setup4);
        }
        catch (Exception ex)
        {
            LogService.Logger.Error($"解析过滤器配置失败: {ex.Message}");
            return new FilterTable();
        }
    }

    /// <summary>
    /// 构建新的操作参数
    /// </summary>
    private static async Task<OperationParameters> BuildParameters(
        OperationParameters parameters,
        string sourcePath,
        string targetPath,
        string rule,
        FilterTable filter,
        string content,
        OperationMode operationMode)
    {
        try
        {
            parameters.SourcePath = string.IsNullOrEmpty(sourcePath) ? parameters.SourcePath : sourcePath;
            parameters.TargetPath = string.IsNullOrEmpty(targetPath) ? parameters.TargetPath : targetPath;

            if (filter != null)
            {
                filter.ContentValue = content.Replace("contains:", "").Replace("regex:", "");
                parameters.RuleModel = new RuleModel()
                {
                    Rule = rule,
                    RuleType = parameters.RuleModel.RuleType,
                    Filter = filter,
                };
            }

            parameters.Funcs = FilterUtil.GeneratePathFilters(rule, parameters.RuleModel.RuleType);
            parameters.OperationMode = operationMode;
            parameters.PathFilter = FilterUtil.GetPathFilters(filter);
            if (operationMode == OperationMode.Rename)
            {
                if (string.IsNullOrEmpty(parameters.TargetPath))
                {
                    parameters.TargetPath = parameters.SourcePath;
                }
                // 获取重命名规则
                parameters.RenamePattern = await GenerateSmartFileNameAsync(parameters);
            }

            return parameters;
        }
        catch (Exception ex)
        {
            LogService.Logger.Error($"构建操作参数失败: {ex.Message}");
            throw;
        }
    }

    private static async Task<string> GenerateSmartFileNameAsync(OperationParameters parameters)
    {
        var systemPrompt = new Prompt("system", PromptConstants.RenameSystemPrompt);
        var customPrompt = FilterUtil.ParseUserDefinePrompt(parameters.Prompt);
        var userPrompt = new Prompt("user", customPrompt);
        var prompts = new List<Prompt> { systemPrompt, userPrompt };

        var userDefinePrompt = new UserDefinePrompt("分类", prompts, true);
        var userDefinePrompts = new List<UserDefinePrompt> { userDefinePrompt };
        parameters.AIServiceLlm.UserDefinePrompts = userDefinePrompts;
        var cts = new CancellationTokenSource();
        await StreamHandlerAsync(
            parameters.AIServiceLlm,
            parameters.SourcePath,
            "",
            cts.Token);
        var renameData = parameters.AIServiceLlm.Data.Result?.ToString() ?? string.Empty;
        var result = FilterUtil.ExtractKeyValuePairsFromRaw(renameData);
        result.TryGetValue("renamePattern", out string renamePattern);
        return renamePattern;
    }

    /// <summary>
    /// 执行AI分类的主要流程
    /// </summary>
    private static async Task CreateAIClassification(OperationParameters parameters)
    {
        var cts = new CancellationTokenSource();
        var customPrompt = FilterUtil.ParseUserDefinePrompt(parameters.Prompt);
        var prompts = InitializePrompts(parameters);

        try
        {
            // 步骤1：判断操作类型
            var step1Result = await ExecuteStepOne(parameters.AIServiceLlm, customPrompt, parameters.Language, cts.Token);

            // 步骤2：判断路径信息
            var step2Result = await UpdatePromptAndExecute(
                parameters,
                prompts,
                PromptConstants.SetupTwoPrompt,
                customPrompt,
                cts,
                "步骤2 - 判断路径信息",
                step1Result);

            // 步骤3：判断文件类型
            var step3Result = await UpdatePromptAndExecute(
                parameters,
                prompts,
                PromptConstants.SetupThreePrompt,
                customPrompt,
                cts,
                "步骤3 - 判断文件类型",
                step1Result);
            if (string.IsNullOrEmpty(step3Result.Rule))
            {
                cts.Cancel();
                return;
            }

            // 步骤4：获取过滤器配置（如果需要）
            FilterTable filterTable = null;
            if (step3Result != null &&
                !string.IsNullOrEmpty(step3Result.RawResult) &&
                !string.IsNullOrEmpty(step3Result.Filter))
            {
                filterTable = await ExecuteStepFour(
                    parameters.AIServiceLlm,
                    prompts,
                    step3Result.Filter,
                    parameters.Language,
                    cts.Token);
            }
            
            parameters.RuleModel.RuleType = step3Result.RuleType;

            // 构建新的操作参数并执行
            var newParameters = await BuildParameters(
                parameters,
                step2Result.SourcePath,
                step2Result.TargetPath,
                step3Result.Rule,
                filterTable,
                step3Result.Content,
                step2Result.OperationMode);

            await ProcessDirectoryAsync(newParameters);
        }
        catch (Exception ex)
        {
            LogService.Logger.Error($"AI 分类执行错误: {ex.Message}");
            throw;
        }
        finally
        {
            cts.Cancel();
        }
    }

    /// <summary>
    /// 更新提示并执行AI分类
    /// </summary>
    private static async Task<AIClassificationResult> UpdatePromptAndExecute(
        OperationParameters parameters,
        List<Prompt> prompts,
        string newPromptContent,
        string customPrompt,
        CancellationTokenSource cts,
        string stepDescription,
        Dictionary<string, string>? keyValuePairs = null)
    {
        try
        {
            var systemPromptToUpdate = prompts.FirstOrDefault(p => p.Role.Equals("system"));
            if (systemPromptToUpdate == null)
            {
                LogService.Logger.Error("未找到用户提示配置");
                throw new InvalidOperationException("未找到用户提示配置");
            }

            systemPromptToUpdate.Content = newPromptContent;
            var userPromptToUpdate = prompts.FirstOrDefault(p => p.Role.Equals("user"));
            userPromptToUpdate.Content = "Here are my requirements: \r\n $source";
            await StreamHandlerAsync(parameters.AIServiceLlm, customPrompt, parameters.Language, cts.Token);

            var result = parameters.AIServiceLlm.Data.Result?.ToString() ?? string.Empty;
            var classificationResult = new AIClassificationResult
            {
                RawResult = result
            };

            var res = FilterUtil.ExtractKeyValuePairsFromRaw(result);
            if (keyValuePairs != null && keyValuePairs.TryGetValue("operation", out string type))
            {
                classificationResult.OperationMode = (OperationMode)EnumHelper.ParseEnum<OperationMode>(type);
                res.TryGetValue("included", out string included);
                classificationResult.IsIncluded = included?.Equals("Y") ?? false;

                if (classificationResult.IsIncluded)
                {
                    res.TryGetValue("sourcePath", out string sourcePath);
                    res.TryGetValue("destinationPath", out string targetPath);
                    classificationResult.SourcePath = sourcePath;
                    classificationResult.TargetPath = targetPath;
                }

                res.TryGetValue("rule", out string rule);
                res.TryGetValue("filter", out string filter);
                res.TryGetValue("content", out string content);
                res.TryGetValue("ruleType", out string ruleType);

                classificationResult.Rule = rule;
                classificationResult.Filter = filter;
                classificationResult.Content = content;
                if (Enum.TryParse(ruleType, out TaskRuleType parsedRuleType))
                {
                    classificationResult.RuleType = parsedRuleType;
                }
            }

            LogService.Logger.Info($"{stepDescription}: {res}");
            return classificationResult;
        }
        catch (Exception ex)
        {
            LogService.Logger.Error($"更新提示并执行失败: {ex.Message}");
            throw;
        }
    }

    private static async Task CreateAISummary(OperationParameters parameters)
    {
        if (Path.GetFileName(parameters.SourcePath).Contains("AISummaryText".GetLocalized())) return;

        var cts = new CancellationTokenSource();
        var fileType = FileReader.GetFileType(parameters.SourcePath);
        string content = string.Empty;
        switch (fileType)
        {
            case FileType.Xls:
            case FileType.Xlsx:
                var contents = FileReader.ReadExcel(parameters.SourcePath);
                foreach (var item in contents)
                {
                    content += item;
                }
                break;
            case FileType.Doc:
            case FileType.Docx:
                content = FileReader.ReadWord(parameters.SourcePath);
                break;
            case FileType.Pdf:
                content = FileReader.ExtractTextFromPdf(parameters.SourcePath);
                break;
            case FileType.Txt:
                content = FileReader.ReadTxt(parameters.SourcePath);
                break;
            default:
                LogService.Logger.Error($"不支持的文件类型: {fileType}");
                cts.Cancel();
                return;
        }
        await StreamHandlerAsync(parameters.AIServiceLlm, content, parameters.Language, cts.Token);
        var oldName = Path.GetFileNameWithoutExtension(parameters.SourcePath);
        string formattedTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        string fileName = $"{"AISummaryText".GetLocalized()}-{formattedTime}-{oldName}.pdf";
        string filePath = Path.Combine(parameters.OldTargetPath, fileName);
        if (parameters.AIServiceLlm.Data.IsSuccess)
        {
            FileWriterUtil.WriteObjectToPdf(parameters.AIServiceLlm.Data.Result, filePath, oldName);
        }

    }

    /// <summary>
    /// 移动文件
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    /// <param name="fileOperationType"></param>
    /// <returns></returns>
    private static async Task MoveFile([Description("源文件/目录路径")] string sourcePath, [Description("目标文件/目录路径")] string targetPath, FileOperationType fileOperationType)
    {
        await _semaphore.WaitAsync(); // 请求对文件操作的独占访问
        try
        {
            FileResolver.HandleFileConflict(sourcePath, targetPath, fileOperationType, () =>
            {
                File.Move(sourcePath, targetPath, true);
            }, true);
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
    private static async Task RenameFile(string sourcePath, string targetPath, OperationParameters parameters = null)
    {
        await _semaphore.WaitAsync(); // 请求对文件操作的独占访问
        try
        {
            string newPath;
            if ("UNSUPPORTED".Equals(parameters.RenamePattern))
            {
                // 使用AI直接重命名
                newPath = await AIRenameFile(sourcePath, parameters);
            }
            else
            {
                newPath = Renamer.ParseTemplate(sourcePath, targetPath);
            }
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

    private static async Task<string> AIRenameFile(string sourcePath, OperationParameters parameters)
    {
        var systemPrompt = new Prompt("system", PromptConstants.AIRenameFilePrompt);
        var userPrompt = new Prompt("user", PromptConstants.AIRenameUserPrompt);
        var prompts = new List<Prompt> { systemPrompt, userPrompt };

        var userDefinePrompt = new UserDefinePrompt("分类", prompts, true);
        var userDefinePrompts = new List<UserDefinePrompt> { userDefinePrompt };
        parameters.AIServiceLlm.UserDefinePrompts = userDefinePrompts;
        var customPrompt = FilterUtil.ParseUserDefinePrompt(parameters.Prompt);
        await StreamHandlerAsync(parameters.AIServiceLlm, sourcePath, customPrompt, default);
        var newName = parameters.AIServiceLlm.Data.Result?.ToString() ?? string.Empty;
        return newName;
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
    internal static async Task MoveToRecycleBin(string path, List<FilterItem> dynamicFilters,
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

    private static async Task ExecuteExternal(string filePath, string arguments = "", string workingDirectory = "", int timeout = 60000)
    {
        string result = await RunAsync(filePath, arguments, workingDirectory, timeout);
        var commandDescription = Path.GetFileName(filePath);
        await IsRunResultSuccessAsync(result, $"执行 {commandDescription}");
    }

    private static async Task IsRunResultSuccessAsync(string result, string commandDescription = "")
    {
        await Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(result))
            {
                LogService.Logger.Warn($"外部命令 [{commandDescription}] 返回为空，可能执行失败。");
            }

            var lower = result.ToLowerInvariant();

            if (lower.Contains("error") || lower.Contains("fail") ||
                lower.Contains("未找到") || lower.Contains("not recognized"))
            {
                LogService.Logger.Error($"外部命令 [{commandDescription}] 执行失败，返回信息：{result}");
            }

            LogService.Logger.Info($"外部命令 [{commandDescription}] 执行成功。");
        });
    }

    private static async Task<string> RunAsync(string filePath, string arguments = "", string workingDirectory = "", int timeout = 60000)
    {
        var output = new StringBuilder();
        var error = new StringBuilder();

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = filePath,
                Arguments = arguments,
                WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? Environment.CurrentDirectory : workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                output.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (await Task.Run(() => process.WaitForExit(timeout)))
        {
            return output.Length > 0 ? output.ToString() : error.ToString();
        }
        else
        {
            process.Kill();
            throw new TimeoutException("外部程序执行超时");
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
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }

    private static async Task StreamHandlerAsync(IAIServiceLlm aIServiceLlm, string content, string lang, CancellationToken token)
    {
        aIServiceLlm.Data = ServiceResult.Reset;

        await aIServiceLlm.PredictAsync(
            new RequestModel(content, lang),
            msg =>
            {
                LogService.Logger.Debug($"AI 服务返回: {msg}");
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
