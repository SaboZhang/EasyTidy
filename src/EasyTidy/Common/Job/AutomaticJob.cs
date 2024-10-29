using EasyTidy.Common.Database;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.Util;
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
        Logger.Info(context.JobDetail.ToString());
        // 获取数据库枚举值
        await OperationHandler.ExecuteOperationAsync(OperationMode.RecycleBin, "示例参数1");
    }

    public static async Task AddTaskConfig(AutomaticTable automaticTable, bool customSchedule = false)
    {
        if (automaticTable != null)
        {
            if (customSchedule)
            {
                if (!string.IsNullOrEmpty(automaticTable.Schedule.CronExpression) && !automaticTable.IsFileChange)
                {
                    foreach (var item in automaticTable.TaskOrchestrationList)
                    {
                        await QuartzHelper.AddJob<AutomaticJob>(item.TaskName + "-" + item.ID, item.GroupName.GroupName, automaticTable.Schedule.CronExpression);
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
                            await QuartzHelper.AddJob<AutomaticJob>(item.TaskName + "-" + item.ID, item.GroupName.GroupName, cronExpression);
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
                    await QuartzHelper.AddSimpleJobOfMinuteAsync<AutomaticJob>(item.TaskName, item.GroupName.GroupName, interval, param);

                }
            }
        }
    }
}
