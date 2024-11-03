using CommunityToolkit.WinUI;
using EasyTidy.Common.Database;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.Util;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System;
using System.Threading.Tasks;


namespace EasyTidy.Common.Job;

public class AutomaticJob : IJob
{
    private readonly AppDbContext _dbContext;

    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public AutomaticJob()
    {
        _dbContext = App.GetService<AppDbContext>();
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var taskId = context.MergedJobDataMap.GetString("TaskId");
        var task = await GetTaskAsync(taskId, context);

        if (task == null)
        {
            Logger.Error("Task retrieval failed or Task is null.");
            return;
        }

        var operationParameters = new OperationParameters(
            operationMode: task.OperationMode,
            sourcePath: task.TaskSource.Equals("DesktopText".GetLocalized())
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : task.TaskSource,
            targetPath: task.TaskTarget,
            fileOperationType: Settings.GeneralConfig.FileOperationType, 
            handleSubfolders: Settings.GeneralConfig.SubFolder ?? false,
            funcs: FilterUtil.GeneratePathFilters(task.TaskRule, task.RuleType),
            pathFilter: FilterUtil.GetPathFilters(task.Filter),
            ruleModel: new RuleModel { Filter = task.Filter, Rule = task.TaskRule, RuleType = task.RuleType },
            id: task.ID,
            ruleName: task.TaskRule
            );

        Logger.Info($"Executing task with SourcePath: {operationParameters.SourcePath}, TargetPath: {operationParameters.TargetPath}");
        // 启动独立的线程来执行操作，避免参数冲突
        await Task.Run(async () =>
        {
            await OperationHandler.ExecuteOperationAsync(task.OperationMode, operationParameters);
        });
    }

    // Helper method to retrieve task based on task ID or job name
    private async Task<TaskOrchestrationTable> GetTaskAsync(string taskId, IJobExecutionContext context)
    {
        if (!string.IsNullOrEmpty(taskId) && int.TryParse(taskId, out int parsedTaskId))
        {
            Logger.Info($"Retrieving task with ID: {parsedTaskId}");
            return await _dbContext.TaskOrchestration.FirstOrDefaultAsync(t => t.ID == parsedTaskId);
        }

        string jobName = context.JobDetail.Key.Name;
        if (string.IsNullOrEmpty(jobName))
        {
            Logger.Error("JobName is null");
            return null;
        }

        var idPart = jobName.Split('#').LastOrDefault();
        if (int.TryParse(idPart, out int parsedId))
        {
            return await _dbContext.TaskOrchestration.FirstOrDefaultAsync(t => t.ID == parsedId);
        }

        Logger.Error($"Failed to parse ID from JobName: {jobName}");
        return null;
    }

    /// <summary>
    /// 添加定时任务
    /// </summary>
    /// <param name="automaticTable"></param>
    /// <param name="customSchedule"></param>
    /// <returns></returns>
    public static async Task AddTaskConfig(AutomaticTable automaticTable, bool customSchedule = false)
    {
        if (automaticTable == null) return;

        var taskOrchestrationList = automaticTable.TaskOrchestrationList;

        // Handle custom scheduling
        if (customSchedule)
        {
            var cronExpression = !string.IsNullOrEmpty(automaticTable.Schedule.CronExpression) && !automaticTable.IsFileChange
                ? automaticTable.Schedule.CronExpression
                : CronExpressionUtil.GenerateCronExpression(automaticTable);

            foreach (var item in taskOrchestrationList)
            {
                await QuartzHelper.AddJob<AutomaticJob>(
                    $"{item.TaskName}#{item.ID}",
                    item.GroupName.GroupName,
                    cronExpression);
            }
            return;
        }

        // Handle file change monitoring and task scheduling
        var interval = (Convert.ToInt32(string.IsNullOrEmpty(automaticTable.Hourly) ? "0" : automaticTable.Hourly) * 60)
             + Convert.ToInt32(string.IsNullOrEmpty(automaticTable.Minutes) ? "0" : automaticTable.Minutes);
        foreach (var item in taskOrchestrationList)
        {
            var param = new Dictionary<string, object> { { "TaskId", item.ID.ToString() } };
            var ruleModel = new RuleModel
            {
                Rule = item.TaskRule,
                RuleType = item.RuleType,
                Filter = item.Filter
            };

            if (automaticTable.IsFileChange)
            {
                var delay = Convert.ToInt32(automaticTable.DelaySeconds);
                var sub = Settings.GeneralConfig?.SubFolder ?? true;
                var parameters = new OperationParameters(
                    operationMode: item.OperationMode,
                    sourcePath: item.TaskSource.Equals("DesktopText".GetLocalized())
                    ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    : item.TaskSource,
                    targetPath: item.TaskTarget,
                    fileOperationType: Settings.GeneralConfig.FileOperationType,
                    handleSubfolders: Settings.GeneralConfig.SubFolder ?? true,
                    funcs: new List<Func<string, bool>>(FilterUtil.GeneratePathFilters(item.TaskRule, item.RuleType)),
                    pathFilter: FilterUtil.GetPathFilters(item.Filter)
                    );
                FileEventHandler.MonitorFolder(parameters, delay);

                if (automaticTable.RegularTaskRunning)
                {
                    await QuartzHelper.AddSimpleJobOfMinuteAsync<AutomaticJob>($"{item.TaskName}#{item.ID}", item.GroupName.GroupName, interval, param);
                }
            }
            else if (automaticTable.RegularTaskRunning)
            {
                await QuartzHelper.AddSimpleJobOfMinuteAsync<AutomaticJob>($"{item.TaskName}#{item.ID}", item.GroupName.GroupName, interval, param);
            }
        }
    }

}
