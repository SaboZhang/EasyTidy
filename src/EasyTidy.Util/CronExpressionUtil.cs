using EasyTidy.Model;
using Quartz;
using Quartz.Impl.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyTidy.Util;

public class CronExpressionUtil
{
    public static string GenerateCronExpression(AutomaticTable automaticTable, bool repair = false)
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

        // 检查条件并设置 Cron 表达式
        if (repair)
        {
            if (!string.IsNullOrWhiteSpace(dayOfMonth)
                && !string.IsNullOrWhiteSpace(month)
                && !string.IsNullOrWhiteSpace(dayOfWeek))
            {
                // 当有具体的日、月和周时，使用 "?" 来忽略周几或具体日
                dayOfWeek = "?"; // 忽略周几
            }
            else if (!string.IsNullOrWhiteSpace(dayOfMonth) && !string.IsNullOrWhiteSpace(month))
            {
                dayOfWeek = "?"; // 如果有特定日和特定月，忽略周几
            }
            else if (!string.IsNullOrWhiteSpace(dayOfWeek))
            {
                dayOfMonth = "?"; // 如果有特定周几，忽略具体日
            }
        }

        // 拼接 Cron 表达式
        string cronExpression = $"{seconds} {minutes} {hours} {dayOfMonth} {month} {dayOfWeek}";
        return cronExpression;
    }

    public static string GenerateCronExpression(string dialogMinutes, string dialogHours, string dialogDayOfMonth, string dialogMonth, string dialogDayOfWeek, bool repair = false)
    {
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
        string minutes = ProcessCronField(dialogMinutes); // 如果没有定义，使用 "*" 表示每分钟
        string hours = ProcessCronField(dialogHours); // 如果没有定义，使用 "*" 表示每小时
        string dayOfMonth = ProcessCronField(dialogDayOfMonth); // 没有定义时，表示每天
        string month = ProcessCronField(dialogMonth); // 没有定义时，表示每月
        string dayOfWeek = dialogDayOfWeek ?? "?"; // 使用 "?" 忽略周几

        // 检查是否为每月的特定日
        if (string.IsNullOrEmpty(dialogMonth))
        {
            dayOfWeek = "?"; // 如果有特定月份，则忽略周几
        }
        else if (string.IsNullOrEmpty(dialogDayOfWeek))
        {
            dayOfMonth = "?"; // 如果有特定周几，则忽略具体日
        }

        // 检查条件并设置 Cron 表达式
        if (repair)
        {
            if (!string.IsNullOrWhiteSpace(dayOfMonth) 
                && !string.IsNullOrWhiteSpace(month) 
                && !string.IsNullOrWhiteSpace(dayOfWeek))
            {
                // 当有具体的日、月和周时，使用 "?" 来忽略周几或具体日
                dayOfWeek = "?"; // 忽略周几
            }
            else if (!string.IsNullOrWhiteSpace(dayOfMonth) && !string.IsNullOrWhiteSpace(month))
            {
                dayOfWeek = "?"; // 如果有特定日和特定月，忽略周几
            }
            else if (!string.IsNullOrWhiteSpace(dayOfWeek))
            {
                dayOfMonth = "?"; // 如果有特定周几，忽略具体日
            }
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
            // 创建 CronTrigger 实例并设置 Cron 表达式
            CronTriggerImpl cronTriggerImpl = new();
            CronExpression cron = new(cronExpression);
            cronTriggerImpl.CronExpression = cron;

            // 计算触发时间
            var dates = TriggerUtils.ComputeFireTimes(cronTriggerImpl, null, 8);

            // 将 DateTimeOffset 转换为 DateTime
            List<DateTime> fireTimes = dates.Select(dt => dt.DateTime).ToList();


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
