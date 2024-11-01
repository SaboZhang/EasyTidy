using EasyTidy.Common.Database;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.Util;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System;


namespace EasyTidy.Common.Job;

public class AutomaticJob : IJob
{
    private readonly AppDbContext _dbContext;

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

        List<Func<string, bool>> pathFilters = BuildFilters(task);

        OperationParameters operationParameters = new OperationParameters
        {
            OperationMode = task.OperationMode,
            SourcePath = task.TaskSource,
            TargetPath = task.TaskTarget,
            FileOperationType = Settings.GeneralConfig.FileOperationType,
            HandleSubfolders = Settings.GeneralConfig.SubFolder,
            Funcs = pathFilters,
        };

        // Execute the operation based on parameters
        await OperationHandler.ExecuteOperationAsync(task.OperationMode, operationParameters);
    }

    // Helper method to retrieve task based on task ID or job name
    private async Task<TaskOrchestrationTable> GetTaskAsync(string taskId, IJobExecutionContext context)
    {
        if (!string.IsNullOrEmpty(taskId) && int.TryParse(taskId, out int parsedTaskId))
        {
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

    private List<Func<string, bool>> BuildFilters(TaskOrchestrationTable task)
    {
        List<Func<string, bool>> pathFilters = FilterUtil.GetPathFilters(task.Filter);
        List<Func<string, bool>> dynamicFilters = FilterUtil.GeneratePathFilters(task.TaskRule, task.RuleType);
        pathFilters.AddRange(dynamicFilters);
        return pathFilters;
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
                FileEventHandler.MonitorFolder(item.OperationMode, item.TaskSource, item.TaskTarget, delay, ruleModel, sub, Settings.GeneralConfig.FileOperationType);

                if (automaticTable.RegularTaskRunning)
                {
                    await QuartzHelper.AddSimpleJobOfMinuteAsync<AutomaticJob>(item.TaskName, item.GroupName.GroupName, interval, param);
                }
            }
            else if (automaticTable.RegularTaskRunning)
            {
                await QuartzHelper.AddSimpleJobOfMinuteAsync<AutomaticJob>(item.TaskName, item.GroupName.GroupName, interval, param);
            }
        }
    }

}
