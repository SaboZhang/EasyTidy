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
        await Task.Run(() => 
        {
            var snapshot = FileResolver.GetDirectorySnapshot(parameters.SourcePath);
            string templatePath = Path.Combine(Constants.ExecutePath, "Assets", "Modules", "template.html");
            if (!parameters.TargetPath.EndsWith(".html"))
            {
                parameters.TargetPath = Path.Combine(parameters.TargetPath, "exportFiles.html");
            }
            GenerateHtmlFromFileSystem(templatePath, parameters.TargetPath ,snapshot);
        });
        LogService.Logger.Info("目录快照创建成功");
    }

    public static void GenerateHtmlFromFileSystem(string templatePath, string outputPath, FileSystemNode rootNode)
    {
        // 读取模板文件
        string templateContent;
        using (StreamReader reader = new StreamReader(templatePath))
        {
            templateContent = reader.ReadToEnd();
        }

        // 替换模板中的占位符
        templateContent = templateContent
            .Replace("[TITLE]", "File System Snapshot")
            .Replace("[APP LINK]", "http://example.com")
            .Replace("[APP NAME]", "FileSystemSnapshotGenerator")
            .Replace("[APP VER]", "1.0")
            .Replace("[GEN TIME]", DateTime.Now.ToString("t"))
            .Replace("[GEN DATE]", DateTime.Now.ToString("d"))
            .Replace("[NUM FILES]", rootNode.FileCount.ToString())
            .Replace("[NUM DIRS]", rootNode.FolderCount.ToString())
            .Replace("[TOT SIZE]", rootNode.Size.ToString())
            .Replace( "[LINK FILES]", "false" )
			.Replace( "[LINK PROTOCOL]", "" )
			.Replace( "[LINK ROOT]", "" )
			.Replace( "[SOURCE ROOT]", rootNode.Name.Replace( @"\", "/" ) );

        // 构建文件系统树的 JavaScript 内容
        var jsContent = BuildJavaScriptContent(rootNode);

        // 插入 JavaScript 内容到模板
        templateContent = templateContent.Replace("[DIR DATA]", jsContent);

        // 写入最终 HTML 到输出文件
        using (StreamWriter writer = new StreamWriter(outputPath, false, Encoding.UTF8))
        {
            writer.Write(templateContent);
        }
    }

    private static string BuildJsonTree(FileSystemNode rootNode)
    {
        Dictionary<string, JsTreeNode> nodeDictionary = new Dictionary<string, JsTreeNode>();
        Queue<(FileSystemNode Node, string ParentId)> queue = new Queue<(FileSystemNode, string)>();

        // 初始化队列，从根节点开始
        string rootId = Guid.NewGuid().ToString();
        queue.Enqueue((rootNode, "#"));

        while (queue.Count > 0)
        {
            var (node, parentId) = queue.Dequeue();

            // 创建当前节点
            var jsTreeNode = new JsTreeNode
            {
                id = Guid.NewGuid().ToString(),
                parent = parentId,
                text = node.Name + (node.Size.HasValue ? $" ({node.Size} bytes)" : ""),
                children = node.IsFolder && node.Children.Any() ? node.Children.Select(c => Guid.NewGuid().ToString()).ToList() : null
            };

            // 将节点加入字典以便后续引用
            nodeDictionary[jsTreeNode.id] = jsTreeNode;

            // 对于每个子节点，增加缩进级别并加入队列
            foreach (var child in node.Children)
            {
                queue.Enqueue((child, jsTreeNode.id));
            }
        }

        // 更新子节点ID，确保它们指向正确的父节点
        foreach (var kvp in nodeDictionary)
        {
            if (kvp.Value.children != null)
            {
                // 更新子节点ID为实际存在的节点ID
                kvp.Value.children = kvp.Value.children.Select(childId =>
                {
                    var childNode = nodeDictionary.FirstOrDefault(n => n.Value.parent == kvp.Key);
                    return childNode.Value?.id ?? childId; // 如果找不到匹配的子节点，则保留原始ID
                }).ToList();
            }
        }

        // 将树节点序列化为 JSON 字符串
        return JsonConvert.SerializeObject(nodeDictionary.Values.ToList(), Formatting.None);
    }

    private static string BuildJavaScriptContent(FileSystemNode rootNode)
    {
        // 使用 StringBuilder 收集所有节点的 D.p 数据
        StringBuilder jsBuilder = new StringBuilder();
        Queue<(FileSystemNode Node, string ParentPath)> queue = new Queue<(FileSystemNode, string)>();

        // 初始化队列
        queue.Enqueue((rootNode, string.Empty));

        while (queue.Count > 0)
        {
            var (node, parentPath) = queue.Dequeue();

            // 当前节点路径
            string nodePath = parentPath == string.Empty
                ? node.Path.Replace("\\", "/")
                : $"{parentPath}/{node.Name}";

            // 构建当前节点的基础信息
            StringBuilder nodeData = new StringBuilder();
            nodeData.Append($"D.p(['{nodePath}*0*{FilterUtil.ToUnixTimestamp(node.ModifiedDate)}',");

            // 子文件夹索引和文件数据
            List<string> childIndices = new List<string>();
            List<string> fileData = new List<string>();
            long totalSize = 0;

            for (int i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                if (child.IsFolder)
                {
                    // 子文件夹：加入队列并记录索引
                    queue.Enqueue((child, nodePath));
                    childIndices.Add(i.ToString());
                }
                else
                {
                    // 子文件：记录文件数据
                    fileData.Add($"'{child.Name}*{child.Size}*{FilterUtil.ToUnixTimestamp(child.ModifiedDate)}'");
                    totalSize += (long)child.Size;
                }
            }

            // 计算总大小（包括当前文件夹大小）
            totalSize += node.Size ?? 0;

            // 拼接当前节点的数据
            nodeData.Append(string.Join(",", fileData));
            nodeData.Append($",{totalSize},'{string.Join("*", childIndices)}'])");

            // 将节点数据追加到结果
            jsBuilder.AppendLine(nodeData.ToString());
        }

        return jsBuilder.ToString();
    }
}
