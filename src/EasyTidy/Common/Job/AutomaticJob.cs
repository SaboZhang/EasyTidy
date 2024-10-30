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
        TaskOrchestrationTable task;
        var taskId = context.MergedJobDataMap.GetString("TaskId");
        if (!string.IsNullOrEmpty(taskId))
        {
            task = _dbContext.TaskOrchestration.Where(t => t.ID == Convert.ToInt32(taskId)).FirstOrDefaultAsync().Result;
        }
        else
        {
            // 获取 JobName
            string jobName = context.JobDetail.Key.Name;
            if (string.IsNullOrEmpty(jobName))
            {
                Logger.Error("JobName is null");
                return;
            }
            var id = jobName.Split('#').LastOrDefault();
            if (id != null && int.TryParse(id, out int parsedId))
            {
                task = await _dbContext.TaskOrchestration
                    .FirstOrDefaultAsync(t => t.ID == parsedId);
            }
            else
            {
                Logger.Error($"ID 解析失败: {jobName}");
                return; // Exit if ID is invalid 
            }
        }
        if (task != null)
        {
            OperationParameters operationParameters = new OperationParameters
            {
                SourcePath = task.TaskSource,
                TargetPath = task.TaskTarget
            };

            // 执行操作
            await OperationHandler.ExecuteOperationAsync(task.OperationMode, operationParameters);
        }else
        {
            Logger.Error("Task is null");
        }
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
        var delay = Convert.ToInt32(automaticTable.DelaySeconds);
        var interval = (Convert.ToInt32(automaticTable.Hourly) * 60) + Convert.ToInt32(automaticTable.Minutes);

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
                FileEventHandler.MonitorFolder(item.OperationMode, item.TaskSource, item.TaskTarget, delay, ruleModel, Settings.GeneralConfig.FileOperationType);

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
