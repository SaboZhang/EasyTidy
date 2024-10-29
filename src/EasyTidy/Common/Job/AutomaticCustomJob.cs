using EasyTidy.Common.Database;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.Util;
using Microsoft.EntityFrameworkCore;
using Quartz;


namespace EasyTidy.Common.Job;

public class AutomaticCustomJob : IJob
{
    private readonly AppDbContext _dbContext;

    public AutomaticCustomJob()
    {
        _dbContext = App.GetService<AppDbContext>();
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var task = new TaskOrchestrationTable();
        var taskId = context.MergedJobDataMap.GetString("TaskId");
        if (!string.IsNullOrEmpty(taskId))
        {
            task = _dbContext.TaskOrchestration.Where(t => t.ID == Convert.ToInt32(taskId)).FirstOrDefaultAsync().Result;
        }
        else
        {
            // 获取 JobName
            string jobName = context.JobDetail.Key.Name;
            task = _dbContext.TaskOrchestration.Where(t => t.TaskName == jobName).FirstOrDefaultAsync().Result;
        }
        // 执行操作
        await OperationHandler.ExecuteOperationAsync(task.OperationMode, "示例参数1");
    }

    /// <summary>
    /// 添加定时任务
    /// </summary>
    /// <param name="automaticTable"></param>
    /// <param name="customSchedule"></param>
    /// <returns></returns>
    public static async Task AddCustomTaskConfig(AutomaticTable automaticTable, bool customSchedule = false)
    {
        if (automaticTable != null)
        {
            if (customSchedule)
            {
                if (!string.IsNullOrEmpty(automaticTable.Schedule.CronExpression) && !automaticTable.IsFileChange)
                {
                    foreach (var item in automaticTable.TaskOrchestrationList)
                    {
                        await QuartzHelper.AddJob<AutomaticCustomJob>(item.TaskName + "-" + item.ID, item.GroupName.GroupName, automaticTable.Schedule.CronExpression);
                    }
                }
                else
                {
                    foreach (var item in automaticTable.TaskOrchestrationList)
                    {

                        if (automaticTable.Schedule.Monthly != null
                            || automaticTable.Schedule.DailyInMonthNumber != null
                            || automaticTable.Schedule.WeeklyDayNumber != null
                            || automaticTable.Schedule.Hours != null
                            || automaticTable.Schedule.Minutes != null)
                        {
                            var cronExpression = CronExpressionUtil.GenerateCronExpression(automaticTable);
                            await QuartzHelper.AddJob<AutomaticCustomJob>(item.TaskName + "-" + item.ID, item.GroupName.GroupName, cronExpression);
                        }
                    }
                }
            }
            else if (automaticTable.IsFileChange)
            {
                foreach (var item in automaticTable.TaskOrchestrationList)
                {
                    var delay = Convert.ToInt32(automaticTable.DelaySeconds);
                    var ruleModel = new RuleModel
                    {
                        Rule = item.TaskRule,
                        RuleType = item.RuleType,
                        Filter = item.Filter
                    };
                    FileEventHandler.MonitorFolder(item.OperationMode, item.TaskSource, item.TaskTarget, delay, Settings.GeneralConfig.FileOperationType, ruleModel);
                }
            }
            else if (automaticTable.RegularTaskRunning)
            {
                foreach (var item in automaticTable.TaskOrchestrationList)
                {
                    var param = new Dictionary<string, object>
                        {
                            { "TaskId", item.ID.ToString() }
                        };
                    var interval = (Convert.ToInt32(automaticTable.Hourly) * 60) + Convert.ToInt32(automaticTable.Minutes);
                    await QuartzHelper.AddSimpleJobOfMinuteAsync<AutomaticCustomJob>(item.TaskName, item.GroupName.GroupName, interval, param);

                }
            }
        }
    }
}
