using CommunityToolkit.WinUI;
using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            { OperationMode.Extract, ExtractAsync },
            { OperationMode.ZipFile, CompressedFileAsync },
            { OperationMode.UploadWebDAV, UploadFileAsync },
            { OperationMode.Encryption, EncryptionAsync },
            { OperationMode.HardLink, CreateHandLink },
            { OperationMode.SoftLink, CreateSoftLink },
            { OperationMode.FileSnapshot, CreateSnapshot },
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

    private static async Task MoveAsync(OperationParameters parameter)
    {
        string operationId = $"{parameter.SourcePath}-{parameter.TargetPath}-{parameter.FileOperationType}";

        // 使用锁来确保同一时间只有一个线程可以执行该操作
        lock (_executedOperations)
        {
            if (_executedOperations.Contains(operationId))
            {
                LogService.Logger.Info($"Move operation already in progress, skipping execution. {parameter.TargetPath}");
                return;
            }
            _executedOperations.Add(operationId);
        }

        try
        {
            await Task.Run(async () =>
            {
                if (parameter.RuleModel.RuleType == TaskRuleType.FileRule)
                {
                    // 确保传递 OperationMode 参数
                    await FileActuator.ExecuteFileOperationAsync(parameter);
                }
                else
                {
                    await FolderActuator.ExecuteFolderOperationAsync(parameter);
                }
               
            });

            LogService.Logger.Info("执行移动操作完成");
        }
        catch (Exception ex)
        {
            LogService.Logger.Error($"执行移动操作异常 {ex}");
        }
        finally
        {
            lock (_executedOperations)
            {
                _executedOperations.Remove(operationId);
                LogService.Logger.Debug("Removed operation from executed operations.");
            }
        }
    }

    private static async Task CopyAsync(OperationParameters parameter)
    {
        await Task.Run(async () =>
        {
            if (parameter.RuleModel.RuleType == TaskRuleType.FileRule)
            {
                await FileActuator.ExecuteFileOperationAsync(parameter);
            }
            else
            {
                await FolderActuator.ExecuteFolderOperationAsync(parameter);
            }
        });
        LogService.Logger.Info("执行复制操作完成");
    }

    private static async Task DeleteAsync(OperationParameters parameter)
    {
        await Task.Run(async () =>
        {
            if (parameter.RuleModel.RuleType == TaskRuleType.FileRule)
            {
                await FileActuator.ExecuteFileOperationAsync(parameter);
            }
            else
            {
                await FolderActuator.ExecuteFolderOperationAsync(parameter);
            }
        });
        LogService.Logger.Info("执行删除操作完成");
    }

    private static async Task RenameAsync(OperationParameters parameter)
    {
        await Task.Run(async () =>
        {
            if (parameter.RuleModel.RuleType == TaskRuleType.FileRule)
            {
                await FileActuator.ExecuteFileOperationAsync(parameter);
            }
            else
            {
                await FolderActuator.ExecuteFolderOperationAsync(parameter);
            }
        });
        LogService.Logger.Info("重命名任务执行完成");
    }

    private static async Task RecycleBinAsync(OperationParameters parameter)
    {
        string operationId = $"{parameter.SourcePath}-{parameter.TargetPath}-{parameter.FileOperationType}";

        // 使用锁来确保同一时间只有一个线程可以执行该操作
        lock (_executedOperations)
        {
            if (_executedOperations.Contains(operationId))
            {
                LogService.Logger.Warn($"Move operation already in progress, skipping execution. {parameter.TargetPath}");
                return;
            }
            _executedOperations.Add(operationId);
        }
        try
        {
            await Task.Run(async () =>
            {
                if (parameter.RuleModel.RuleType == TaskRuleType.FileRule)
                {
                    await FileActuator.ExecuteFileOperationAsync(parameter);
                }
                else
                {
                    await FolderActuator.ExecuteFolderOperationAsync(parameter);
                }
            });
            LogService.Logger.Info("执行回收站任务完成");
        }
        finally
        {
            lock (_executedOperations)
            {
                _executedOperations.Remove(operationId);
                LogService.Logger.Debug("Removed operation from executed operations.");
            }
        }

    }

    private static async Task ExtractAsync(OperationParameters parameter)
    {
        if (string.IsNullOrEmpty(parameter.TargetPath))
        {
            parameter.TargetPath = parameter.SourcePath;
        }
        var rule = FilterUtil.CheckAndCollectNonCompressedExtensions(parameter.RuleModel.Rule);
        var model = new RuleModel
        {
            Filter = parameter.RuleModel.Filter,
            Rule = rule,
            RuleType = parameter.RuleModel.RuleType
        };
        parameter.RuleModel = model;
        parameter.Funcs = FilterUtil.GeneratePathFilters(rule,parameter.RuleModel.RuleType);
        await Task.Run(async () =>
        {
            await FileActuator.ExecuteFileOperationAsync(parameter);
        });
        LogService.Logger.Info("执行解压任务完成");
    }

    private static async Task CompressedFileAsync(OperationParameters parameter)
    {
        await Task.Run(async () =>
        {
            if (parameter.RuleModel.RuleType == TaskRuleType.FileRule)
            {
                await FileActuator.ExecuteFileOperationAsync(parameter);
            }
            else
            {
                await FolderActuator.ExecuteFolderOperationAsync(parameter);
            }
        });
        LogService.Logger.Info("执行压缩任务完成");
    }

    private static async Task UploadFileAsync(OperationParameters parameter)
    {
        await Task.Run(async () =>
        {
            if (parameter.RuleModel.RuleType == TaskRuleType.FileRule)
            {
                await FileActuator.ExecuteFileOperationAsync(parameter);
            }
            else
            {
                await FolderActuator.ExecuteFolderOperationAsync(parameter);
            }
        });
        LogService.Logger.Info("执行上传任务完成");
    }

    private static async Task EncryptionAsync(OperationParameters parameters)
    {
        await Task.Run(async () =>
        {
            await FileActuator.ExecuteFileOperationAsync(parameters);
        });
        LogService.Logger.Info("执行加密任务完成");
    }

    private static async Task CreateSoftLink(OperationParameters parameters)
    {
        await Task.Run(async () =>
        {
            if (parameters.RuleModel.RuleType == TaskRuleType.FileRule)
            {
                await FileActuator.ExecuteFileOperationAsync(parameters);
            }
            else
            {
                await FolderActuator.ExecuteFolderOperationAsync(parameters);
            }
        });
        LogService.Logger.Info("执行软连接任务完成");
    }

    private static async Task CreateHandLink(OperationParameters parameters)
    {
        await Task.Run(async () =>
        {
            await FileActuator.ExecuteFileOperationAsync(parameters);
        });
        LogService.Logger.Info("执行硬连接任务完成");
    }

    private static async Task CreateSnapshot(OperationParameters parameters)
    {
        // 使用 ConfigureAwait(false) 避免上下文捕获问题
        await Task.Run(() =>
        {
            // 获取目录快照
            var snapshot = FileResolver.GetDirectorySnapshot(parameters.SourcePath, parameters);

            // 确保目标路径为 HTML 文件
            string templatePath = Path.Combine(Constants.ExecutePath, "Assets", "Modules", "template.html");
            if (!parameters.TargetPath.EndsWith(".html"))
            {
                var snapName = Path.GetFileName(parameters.SourcePath);
                snapName = string.IsNullOrWhiteSpace(snapName)
                    ? (Path.GetPathRoot(parameters.SourcePath)?.TrimEnd(Path.DirectorySeparatorChar).Replace(":", string.Empty) ?? "EasyTidySnapshot") + ".html"
                    : snapName + ".html";
                parameters.TargetPath = Path.Combine(parameters.TargetPath, snapName);
            }

            // 生成 HTML
            GenerateHtmlFromFileSystem(templatePath, parameters.TargetPath, snapshot, parameters.RuleModel.Rule);
        }).ConfigureAwait(false);

        LogService.Logger.Info("目录快照创建成功");
    }

    public static void GenerateHtmlFromFileSystem(string templatePath, string outputPath, FileSystemNode rootNode, string rule)
    {
        // 读取模板文件
        string templateContent = File.ReadAllText(templatePath);

        // 替换模板占位符
        templateContent = ReplaceTemplatePlaceholders(templateContent, rootNode);

        // 构建文件系统树的 JavaScript 内容
        string jsContent = BuildJavaScriptContent(rootNode);

        var ruleInfo = string.Format("RuleInfoText".GetLocalized(), rule);

        // 插入 JavaScript 内容到模板
        templateContent = templateContent.Replace("[DIR DATA]", jsContent).Replace("[RULE INFO]", ruleInfo);

        // 写入最终 HTML 到输出文件
        File.WriteAllText(outputPath, templateContent, Encoding.UTF8);
    }

    private static string ReplaceTemplatePlaceholders(string templateContent, FileSystemNode rootNode)
    {
        var title = Constants.AppName + " " + "FileSnapshotText".GetLocalized();
        var fileCount = string.Format("FileCountText".GetLocalized(), rootNode.FileCount.ToString());
        var folderCount = string.Format("FolderCountText".GetLocalized(), rootNode.FolderCount.ToString());
        var genDate = string.Format("GeneratedText".GetLocalized(), DateTime.Now.ToString("yyyy-MM-dd "));
        return templateContent
            .Replace("[TITLE]", title)
            .Replace("[APP LINK]", "https://easytidy.luckyits.com")
            .Replace("[APP NAME]", Constants.AppName)
            .Replace("[APP VER]", Constants.Version)
            .Replace("[GEN TIME]", DateTime.Now.ToString("HH:mm:ss"))
            .Replace("[GEN DATE]", genDate)
            .Replace("[NUM FILES]", fileCount)
            .Replace("[NUM DIRS]", folderCount)
            .Replace("[TOT SIZE]", rootNode.Size.ToString())
            .Replace("[LINK FILES]", "false")
            .Replace("[LINK PROTOCOL]", string.Empty)
            .Replace("[LINK ROOT]", string.Empty)
            .Replace("[SOURCE ROOT]", rootNode.Name.Replace(@"\", "/"));
    }

    private static string BuildJavaScriptContent(FileSystemNode rootNode)
    {
        var jsBuilder = new StringBuilder();
        var queue = new Queue<(FileSystemNode Node, int Index)>();
        var dirIndexMap = new Dictionary<string, string>();
        int globalIndex = 0;

        // 初始化根节点
        if (rootNode != null)
        {
            string rootFullPath = rootNode.Path.Replace("\\", "/");
            dirIndexMap.Add(rootFullPath, globalIndex++.ToString());
            queue.Enqueue((rootNode, int.Parse(dirIndexMap[rootFullPath])));
        }

        while (queue.Count > 0)
        {
            var (node, currentIndex) = queue.Dequeue();
            string nodeFullPath = node.Path.Replace("\\", "/");

            // 当前节点的文件和文件夹信息
            var nodeData = new StringBuilder($"D.p([\"{nodeFullPath}*0*{FilterUtil.ToUnixTimestamp(node.ModifiedDate)}\"");
            long totalSize = 0; // 当前节点的文件总大小
            var fileData = new List<string>(); // 文件信息
            var childIndices = new List<string>(); // 子文件夹索引

            foreach (var child in node.Children)
            {
                string childFullPath = Path.Combine(nodeFullPath, child.Name).Replace("\\", "/");
                if (child.IsFolder)
                {
                    // 为子文件夹分配索引
                    if (!dirIndexMap.ContainsKey(childFullPath))
                    {
                        dirIndexMap[childFullPath] = globalIndex++.ToString();
                    }
                    queue.Enqueue((child, int.Parse(dirIndexMap[childFullPath])));
                    childIndices.Add(dirIndexMap[childFullPath]);
                }
                else
                {
                    // 收集文件信息
                    fileData.Add($"\"{child.Name}*{child.Size}*{FilterUtil.ToUnixTimestamp(child.ModifiedDate)}\"");
                    totalSize += child.Size ?? 0;
                }
            }

            // 拼接文件数据
            if (fileData.Count > 0)
            {
                nodeData.Append(',').Append(string.Join(",", fileData));
            }

            // 添加文件总大小
            nodeData.Append($",{totalSize},");

            // 添加子文件夹索引
            if (childIndices.Count > 0)
            {
                nodeData.Append($"\"{string.Join("*", childIndices)}\"");
            }
            else
            {
                nodeData.Append("\"\"");
            }

            nodeData.Append("])");
            jsBuilder.AppendLine(nodeData.ToString());
        }

        return jsBuilder.ToString();
    }
    
}
