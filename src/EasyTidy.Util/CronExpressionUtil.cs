using EasyTidy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Util;

public class CronExpressionUtil
{
    public static string GenerateCronExpression(AutomaticTable automaticTable)
    {
        // 确保 Schedule 不为空
        if (automaticTable?.Schedule == null)
        {
            throw new ArgumentNullException("Schedule", "Schedule cannot be null.");
        }

        string ProcessCronField(string field, string defaultValue = "*")
        {
            if (string.IsNullOrWhiteSpace(field))
                return defaultValue;

            // Split by comma and process each value
            var values = field.Split(',')
                .Select(v => v.Trim())
                .Select(v => v.Contains('-') || v.Contains('/') ? v : (v == "" ? "*" : v))
                .ToArray();

            return string.Join(",", values);
        }

        // 设置 Cron 表达式的各个部分
        string seconds = "0"; // 固定为0秒
        string minutes = ProcessCronField(automaticTable.Schedule.Minutes); // 如果没有定义，使用 "*" 表示每分钟
        string hours = ProcessCronField(automaticTable.Schedule.Hours); // 如果没有定义，使用 "*" 表示每小时
        string dayOfMonth = ProcessCronField(automaticTable.Schedule.DailyInMonthNumber); // 没有定义时，表示每天
        string month = ProcessCronField(automaticTable.Schedule.Monthly); // 没有定义时，表示每月
        string dayOfWeek = automaticTable.Schedule.WeeklyDayNumber ?? "?"; // 使用 "?" 忽略周几

        // 检查是否为每月的特定日
        if (automaticTable.Schedule.Monthly != null)
        {
            dayOfWeek = "?"; // 如果有特定月份，则忽略周几
        }
        else if (automaticTable.Schedule.WeeklyDayNumber != null)
        {
            dayOfMonth = "?"; // 如果有特定周几，则忽略具体日
        }

        // 拼接 Cron 表达式
        string cronExpression = $"{seconds} {minutes} {hours} {dayOfMonth} {month} {dayOfWeek}";
        return cronExpression;
    }

    /// <summary>
    /// 验证 Cron 表达式是否有效
    /// </summary>
    /// <param name="cronExpression"></param>
    /// <returns></returns>
    public static (bool IsValid, string Message, List<DateTime> FireTimes) VerificationCronExpression(string cronExpression)
    {
        try
        {
            // 创建 Cron 表达式对象
            ICronExpression cron = CronExpression.Parse(cronExpression);

            // 获取当前时间
            DateTime now = DateTime.UtcNow;
            // 存储最近五次的执行时间
            var fireTimes = new List<DateTime>();
            // 计算最近五次的执行时间
            for (int i = 0; i < 5; i++)
            {
                DateTime? nextFireTime = cron.GetPreviousFireTimeUtc(now);
                if (nextFireTime.HasValue)
                {
                    fireTimes.Add(nextFireTime.Value.ToLocalTime());
                    now = nextFireTime.Value;
                }
                else
                {
                    break;
                }
            }

            // 输出结果
            if (fireTimes.Count > 0)
            {
                return (true, "Cron expression is valid. Last five execution times:", fireTimes);
            }
            else
            {
                return (true, "Cron expression is valid but no execution times found.", fireTimes);
            }
        }
        catch (Exception ex)
        {
            return (false, ex.Message, new List<DateTime>());
        }
    }

}
