using EasyTidy.Log;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyTidy.Service;

public class QuartzHelper
{
    private static IScheduler _scheduler = null;

    // 设置 Scheduler 实例
    public static void SetScheduler(IScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    public static async Task AddSimpleJobOfSecondAsync<T>(string jobName, string groupName, int second, Dictionary<string, object> param = null, int priority = 5) where T : IJob
    {
        await AddSimpleJobAsync<T>(jobName, groupName, (x) => { x.WithIntervalInSeconds(second).RepeatForever(); }, param, priority);
    }

    public static async Task AddSimpleJobOfMinuteAsync<T>(string jobName, string groupName, int minute, Dictionary<string, object> param = null, int priority = 5) where T : IJob
    {
        await AddSimpleJobAsync<T>(jobName, groupName, (x) => { x.WithIntervalInMinutes(minute).RepeatForever(); }, param, priority);
    }

    public static async Task AddSimpleJobOfHourAsync<T>(string jobName, string groupName, int hour, Dictionary<string, object> param = null, int priority = 5) where T : IJob
    {
        await AddSimpleJobAsync<T>(jobName, groupName, (x) => { x.WithIntervalInHours(hour).RepeatForever(); }, param, priority);
    }

    public static async Task AddSimpleJobAsync<T>(string jobName, string groupName, Action<SimpleScheduleBuilder> action, Dictionary<string, object> param = null, int priority = 5) where T : IJob
    {
        var jobKey = new JobKey(jobName, groupName);
        if (await _scheduler.CheckExists(jobKey))
        {
            return;
        }

        var job = JobBuilder.Create<T>()
            .WithIdentity(jobKey)
            .Build();

        if (param != null && param.Count > 0)
        {
            foreach (string key in param.Keys)
            {
                job.JobDataMap.Put(key, param[key]);
            }
        }

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"{jobName}Trigger", groupName)
            .WithSimpleSchedule(action)
            .WithPriority(priority)
            .Build();

        await _scheduler.ScheduleJob(job, trigger);
    }

    public static async Task AddJob<T>(string jobName, string groupName, string cronExpression, int priority = 5) where T : IJob
    {
        var jobKey = new JobKey(jobName, groupName);
        if (await _scheduler.CheckExists(jobKey))
        {
            return;
        }

        var job = JobBuilder.Create<T>()
            .WithIdentity(jobKey)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"{jobName}Trigger", groupName)
            .WithCronSchedule(cronExpression)
            .WithPriority(priority)
            .Build();

        await _scheduler.ScheduleJob(job, trigger);
    }

    public static async Task DeleteJob(string jobName, string groupName)
    {
        var jobKey = new JobKey(jobName, groupName);
        if (!await _scheduler.CheckExists(jobKey))
        {
            return;
        }

        await _scheduler.DeleteJob(jobKey);
    }

    public static async Task PauseJob(string jobName, string groupName)
    {
        var jobKey = new JobKey(jobName, groupName);
        if (!await _scheduler.CheckExists(jobKey))
        {
            return;
        }

        await _scheduler.PauseJob(jobKey);
    }

    public static async Task ResumeJob(string jobName, string groupName)
    {
        var jobKey = new JobKey(jobName, groupName);
        if (!await _scheduler.CheckExists(jobKey))
        {
            return;
        }

        await _scheduler.ResumeJob(jobKey);
    }

    public static async Task UpdateJob(string jobName, string groupName, string newJobName, string newGroupName)
    {
        var jobKey = new JobKey(jobName, groupName);
        var newJobKey = new JobKey(newJobName, newGroupName);
        if (jobKey.Equals(newJobKey) || !await _scheduler.CheckExists(jobKey))
        {
            return;
        }
        var jobDetail = await _scheduler.GetJobDetail(jobKey);

        var triggers = await _scheduler.GetTriggersOfJob(jobKey);

        if (jobDetail != null)
        {   
            var newJob = jobDetail.GetJobBuilder()
                .WithIdentity(newJobKey)
                .Build();
            var jobDataMap = jobDetail.JobDataMap; // 获取原任务的参数
            if (jobDataMap != null)
            {
                newJob.JobDataMap.PutAll(jobDataMap);
            }

            foreach (var trigger in triggers)
            {
                var newTriggerKey = new TriggerKey($"{newJobName}Trigger", newGroupName);
                var newTrigger = trigger.GetTriggerBuilder()
                    .WithIdentity(newTriggerKey)
                    .ForJob(newJobKey)
                    .Build();
                await _scheduler.ScheduleJob(newJob, newTrigger);
            }
        }
        await DeleteJob(jobName, groupName);
    }

    public static async Task UpdateTaskPriority(string jobName, string groupName, int newPriority)
    {
        var jobKey = new JobKey(jobName, groupName);
        var triggers = await _scheduler.GetTriggersOfJob(jobKey);

        foreach (var trigger in triggers)
        {
            if (trigger is ITrigger existingTrigger)
            {
                // 构建新的触发器，设置新的优先级
                var newTrigger = existingTrigger.GetTriggerBuilder()
                    .WithPriority(newPriority) // 设置新优先级
                    .Build();

                // 替换旧触发器
                await _scheduler.RescheduleJob(existingTrigger.Key, newTrigger);
            }
        }
    }

    public static async Task TriggerAllJobsOnceAsync()
    {
        // 获取所有作业的触发器
        var jobKeys = await _scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());

        foreach (var jobKey in jobKeys)
        {
            // 获取作业详情
            var jobDetail = await _scheduler.GetJobDetail(jobKey);

            // 触发作业
            await _scheduler.TriggerJob(jobKey);
        }
        LogService.Logger.Info("所有任务触发成功");
    }

    public static async Task StartAllJob()
    {
        await _scheduler.Start();
        LogService.Logger.Info("任务调度器启动成功");
    }

    public static async Task StopAllJob()
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown();
        }
        LogService.Logger.Info("All tasks scheduler stopped successfully.");

    }


}
