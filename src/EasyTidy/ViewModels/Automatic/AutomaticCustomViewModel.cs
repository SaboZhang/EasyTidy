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
                            if (automaticTable.IsFileChange)
                            {
                                var param = new Dictionary<string, object>
                                {
                                    { "taskName", item.TaskName },
                                    { "groupName", item.GroupName.GroupName },
                                    { "TaskId", item.ID }
                                };
                                var delay = Convert.ToInt32(automaticTable.DelaySeconds);
                                await QuartzHelper.AddSimpleJobOfSecondAsync<AutomaticCustomViewModel>(item.TaskName + "-" + item.ID, item.GroupName.GroupName, delay, param);
                            }else if (automaticTable.RegularTaskRunning)
                            {
                                var param = new Dictionary<string, object>
                                {
                                    { "taskName", item.TaskName },
                                    { "groupName", item.GroupName.GroupName },
                                    { "TaskId", item.ID }
                                };
                                var interval = (Convert.ToInt32(automaticTable.Hourly) * 60) + Convert.ToInt32(automaticTable.Minutes);
                                await QuartzHelper.AddSimpleJobOfMinuteAsync<AutomaticCustomViewModel>(item.TaskName, item.GroupName.GroupName, interval, param);
                            }else if (automaticTable.Schedule.Monthly != null 
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
            }
        }


        public async Task Execute(IJobExecutionContext context)
        {
            var taskId = context.MergedJobDataMap.GetString("TaskId");
            var taskName = context.MergedJobDataMap.GetString("taskName");
            var groupName = context.MergedJobDataMap.GetString("groupName");
            // 获取 JobName
            string jobName = context.JobDetail.Key.Name;
            // 获取数据库枚举值
            var mode = 0;
            await OperationHandler.ExecuteOperationAsync(mode, "示例参数1");
        }
    }
}
