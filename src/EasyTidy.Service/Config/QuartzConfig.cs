using EasyTidy.Model;
using Quartz;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Service;

public class QuartzConfig
{
    public static async Task InitQuartzConfigAsync()
    {
        var properties = new NameValueCollection
        {
            ["quartz.scheduler.instanceName"] = "EasyTidyScheduler",
            ["quartz.scheduler.instanceId"] = "AUTO",
            ["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.SQLiteDelegate, Quartz",
            ["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz", // 指定持久化存储策略
            ["quartz.jobStore.tablePrefix"] = "QRTZ_", // 表前缀
        };

        // 使用SchedulerBuilder创建调度器实例，并根据需要覆盖或添加配置
        IScheduler scheduler = await SchedulerBuilder.Create(properties)
            // 默认最大并发度为10，这里修改为5
            .UseDefaultThreadPool(x => x.MaxConcurrency = 5)
            // 作业错过触发时间的阈值为60秒，可以根据需要调整
            .WithMisfireThreshold(TimeSpan.FromSeconds(60))
            // 配置持久化存储策略
            .UsePersistentStore(x =>
            {
                // 强制作业数据映射的值被视为字符串，避免对象意外序列化后格式破坏导致的问题，默认为false
                x.UseProperties = true;
                x.UseSQLite($"Data Source={Constants.CnfPath}/EasyTidy.db");
                x.UseNewtonsoftJsonSerializer();
            })
            // 最后，基于上述配置构建并返回调度器实例
            .BuildScheduler();

        QuartzHelper.SetScheduler(scheduler);
    }
}
