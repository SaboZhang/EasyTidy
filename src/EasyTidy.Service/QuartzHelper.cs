﻿using Quartz;
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
    }

    public static async Task StartAllJob()
    {
        await _scheduler.Start();
    }

    public static async Task StopAllJob()
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown();
        }

    }


}
