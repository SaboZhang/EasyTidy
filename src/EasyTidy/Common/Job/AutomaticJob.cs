﻿using CommunityToolkit.WinUI;
using EasyTidy.Common.Database;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.Util;
using Microsoft.EntityFrameworkCore;
using Quartz;


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

        var rule = await GetSpecialCasesRule(task.GroupName.Id, task.TaskRule);
        var operationParameters = new OperationParameters(
            operationMode: task.OperationMode,
            sourcePath: task.TaskSource.Equals("DesktopText".GetLocalized())
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : task.TaskSource,
            targetPath: task.TaskTarget,
            fileOperationType: Settings.GeneralConfig.FileOperationType,
            handleSubfolders: Settings.GeneralConfig.SubFolder ?? false,
            funcs: FilterUtil.GeneratePathFilters(rule, task.RuleType),
            pathFilter: FilterUtil.GetPathFilters(task.Filter),
            ruleModel: new RuleModel { Filter = task.Filter, Rule = task.TaskRule, RuleType = task.RuleType })
        { RuleName = task.TaskRule };

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
            return await _dbContext.TaskOrchestration
                .Include(t => t.GroupName)
                .FirstOrDefaultAsync(t => t.ID == parsedTaskId && t.IsEnabled == true);
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
            return await _dbContext.TaskOrchestration
                .Include(t => t.GroupName)
                .FirstOrDefaultAsync(t => t.ID == parsedId && t.IsEnabled == true);
        }

        Logger.Error($"Failed to parse ID from JobName: {jobName}");
        return null;
    }

    /// <summary>
    /// 判断分组是否存在"#"或者"##"规则
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="taskRule"></param>
    /// <returns></returns>
    private async Task<string> GetSpecialCasesRule(int groupId, string taskRule)
    {
        if(taskRule.Trim().Equals("#") || taskRule.Trim().Equals("##"))
        {
            var list = await _dbContext.TaskOrchestration.Where(t => t.GroupName.Id == groupId && t.TaskRule != taskRule).ToListAsync();
            string delimiter = "&";
            return taskRule + string.Join(delimiter, list.Select(x => x.TaskRule));
        }
        return taskRule;
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

        // 对任务进行优先级排序，避免简单任务触发时间不一致而优先级不生效
        var taskOrchestrationList = automaticTable.TaskOrchestrationList.OrderBy(x => x.Priority).ToList();

        // Handle custom scheduling
        if (customSchedule)
        {
            var cronExpression = !string.IsNullOrEmpty(automaticTable.Schedule.CronExpression) && !automaticTable.IsFileChange
                ? automaticTable.Schedule.CronExpression
                : CronExpressionUtil.GenerateCronExpression(automaticTable);

            foreach (var item in taskOrchestrationList)
            {
                int priority = item.Priority == 0 ? 10 : 5;
                await QuartzHelper.AddJob<AutomaticJob>(
                    $"{item.TaskName}#{item.ID}",
                    item.GroupName.GroupName,
                    cronExpression,
                    priority);
            }
            return;
        }

        // Handle file change monitoring and task scheduling
        var interval = (Convert.ToInt32(string.IsNullOrEmpty(automaticTable.Hourly) ? "0" : automaticTable.Hourly) * 60)
             + Convert.ToInt32(string.IsNullOrEmpty(automaticTable.Minutes) ? "0" : automaticTable.Minutes);
        foreach (var item in taskOrchestrationList)
        {
            var param = new Dictionary<string, object> { { "TaskId", item.ID.ToString() } };
            int priority = item.Priority == 0 ? 10 : 5;
            var ruleModel = new RuleModel
            {
                Rule = item.TaskRule,
                RuleType = item.RuleType,
                Filter = item.Filter
            };

            if (automaticTable.IsFileChange)
            {
                var delay = Convert.ToInt32(automaticTable.DelaySeconds);
                var sub = Settings.GeneralConfig?.SubFolder ?? false;
                var parameters = new OperationParameters(
                    operationMode: item.OperationMode,
                    sourcePath: item.TaskSource.Equals("DesktopText".GetLocalized())
                    ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    : item.TaskSource,
                    targetPath: item.TaskTarget,
                    fileOperationType: Settings.GeneralConfig.FileOperationType,
                    handleSubfolders: Settings.GeneralConfig.SubFolder ?? false,
                    funcs: new List<Func<string, bool>>(FilterUtil.GeneratePathFilters(item.TaskRule, item.RuleType)),
                    pathFilter: FilterUtil.GetPathFilters(item.Filter))
                {
                    Priority = item.Priority,
                    CreateTime = item.CreateTime
                };
                FileEventHandler.MonitorFolder(parameters, delay);

                if (automaticTable.RegularTaskRunning)
                {
                    await QuartzHelper.AddSimpleJobOfMinuteAsync<AutomaticJob>($"{item.TaskName}#{item.ID}", item.GroupName.GroupName, interval, param, priority);
                }
            }
            else if (automaticTable.RegularTaskRunning)
            {
                await QuartzHelper.AddSimpleJobOfMinuteAsync<AutomaticJob>($"{item.TaskName}#{item.ID}", item.GroupName.GroupName, interval, param, priority);
            }
            // 保证简单定时任务拥有不同的执行起始时间
            await Task.Delay(100);
        }
    }

}