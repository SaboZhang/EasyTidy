using EasyTidy.Common.Database;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.Util;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.ViewModels.Automatic
{
    public class AutomaticCustomViewModel : IJob
    {
        private readonly AppDbContext _dbContext;

        public AutomaticCustomViewModel()
        {
            _dbContext = App.GetService<AppDbContext>();
        }

        public async Task AddCustomTaskConfig(AutomaticTable automaticTable, bool customSchedule = false)
        {
            if (automaticTable != null)
            {
                if (customSchedule)
                {
                    if (!string.IsNullOrEmpty(automaticTable.Schedule.CronExpression) && !automaticTable.IsFileChange)
                    {
                        foreach (var item in automaticTable.TaskOrchestrationList)
                        {
                            await QuartzHelper.AddJob<AutomaticCustomViewModel>(item.TaskName + "-" + item.ID, item.GroupName.GroupName, automaticTable.Schedule.CronExpression);
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
                                await QuartzHelper.AddJob<AutomaticCustomViewModel>(item.TaskName + "-" + item.ID, item.GroupName.GroupName, cronExpression);
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
                            { "TaskId", item.ID }
                        };
                        var interval = (Convert.ToInt32(automaticTable.Hourly) * 60) + Convert.ToInt32(automaticTable.Minutes);
                        await QuartzHelper.AddSimpleJobOfMinuteAsync<AutomaticCustomViewModel>(item.TaskName, item.GroupName.GroupName, interval, param);

                    }
                }
            }
        }


        public async Task Execute(IJobExecutionContext context)
        {
            var taskId = context.MergedJobDataMap.GetString("TaskId");
            if (!string.IsNullOrEmpty(taskId))
            {
                var task = _dbContext.TaskOrchestration.Where(t => t.ID == Convert.ToInt32(taskId)).ToList();
                Logger.Info(task.Count() + "个定时任务被触发");
            }
            // 获取 JobName
            string jobName = context.JobDetail.Key.Name;
            // 获取数据库枚举值
            var mode = 0;
            await OperationHandler.ExecuteOperationAsync(mode, "示例参数1");
        }
    }
}
