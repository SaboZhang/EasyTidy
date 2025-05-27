using CommunityToolkit.WinUI;
using EasyTidy.Common.Database;
using EasyTidy.Model;
using Microsoft.EntityFrameworkCore;

namespace EasyTidy.Service;

public class ShutdownService
{
    private readonly AppDbContext _dbContext;

    public ShutdownService()
    {
        _dbContext = App.GetService<AppDbContext>();
    }

    public static async Task OnShutdownAsync()
    {
        var instance = new ShutdownService();
        await instance.OnShutdownExecutionAsync();
    }
    private async Task OnShutdownExecutionAsync()
    {
        try
        {
            var list = await _dbContext.Automatic.Include(a => a.TaskOrchestrationList).Where(a => a.OnShutdownExecution).ToListAsync();
            if (list.Count == 0) return;

            // 并行执行任务，但不等待
            var tasks = list.SelectMany(item =>
                item.TaskOrchestrationList
                    .Where(t => t.IsRelated == true)
                    .Select(async task =>
                    {

                        // 执行操作
                        await OperationHandler.ExecuteOperationAsync(task.OperationMode, new OperationParameters(
                            task.OperationMode,
                            task.TaskSource = task.TaskSource.Equals("DesktopText".GetLocalized())
                            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : task.TaskSource,
                            task.TaskTarget,
                            Settings.GeneralConfig.FileOperationType,
                            (bool)Settings.GeneralConfig.SubFolder,
                            new List<FilterItem>(FilterUtil.GeneratePathFilters(task.TaskRule, task.RuleType)),
                            FilterUtil.GetPathFilters(task.Filter),
                            new RuleModel()
                            {
                                Rule = task.TaskRule,
                                RuleType = task.RuleType,
                                Filter = task.Filter
                            })
                        {
                            CreateTime = task.CreateTime,
                            Priority = task.Priority
                        });
                    }));

            // 启动所有任务，但不等待它们完成
            _ = Task.WhenAll(tasks);
            Logger.Info($"启动{list.Count}个任务成功（启动时执行的任务）....");
        }
        catch (Exception ex)
        {
            Logger.Error($"启动时执行失败：{ex}");
        }

    }
}
