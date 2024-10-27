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

        // 设置 Cron 表达式的各个部分
        string seconds = "0"; // 固定为0秒
        string minutes = automaticTable.Schedule.Minutes ?? "*"; // 如果没有定义，使用 "*" 表示每分钟
        string hours = automaticTable.Schedule.Hours ?? "*"; // 如果没有定义，使用 "*" 表示每小时
        string dayOfMonth = automaticTable.Schedule.DailyInMonthNumber ?? "*"; // 没有定义时，表示每天
        string month = automaticTable.Schedule.Monthly ?? "*"; // 没有定义时，表示每月
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

}
