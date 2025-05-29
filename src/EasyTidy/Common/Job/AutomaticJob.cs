using CommunityToolkit.WinUI;
using EasyTidy.Common.Database;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.Service.AIService;
using Microsoft.EntityFrameworkCore;
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
        TrayIconService.SetStatus(TrayIconStatus.Running);
        var taskId = context.MergedJobDataMap.GetString("TaskId");
        var task = await GetTaskAsync(taskId, context);

        if (task == null)
        {
            Logger.Error("Task retrieval failed or Task is null.");
            return;
        }

        if (string.IsNullOrEmpty(task.TaskSource))
        {
            Logger.Warn("源文件夹为空，此次自动任务将退出执行");
            return;
        }

        var rule = await GetSpecialCasesRule(task.GroupName.Id, task.TaskRule);
        string language = string.IsNullOrEmpty(Settings.Language) ? "Follow the document language" : Settings.Language;
        var ai = await _dbContext.AIService.Where(x => x.Identify.ToString().ToLower().Equals(task.AIIdentify.ToString().ToLower())).FirstOrDefaultAsync();
        IAIServiceLlm llm = null;
        if (task.OperationMode == OperationMode.AIClassification || task.OperationMode == OperationMode.AISummary)
        {
            llm = AIServiceFactory.CreateAIServiceLlm(ai, task.UserDefinePromptsJson);
        }
        var operationParameters = new OperationParameters(
            operationMode: task.OperationMode,
            sourcePath: task.TaskSource.Equals("DesktopText".GetLocalized())
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : task.TaskSource,
            targetPath: task.TaskTarget.Equals("DesktopText".GetLocalized())
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : task.TaskTarget,
            fileOperationType: Settings.GeneralConfig.FileOperationType,
            handleSubfolders: Settings.GeneralConfig.SubFolder ?? false,
            funcs: FilterUtil.GeneratePathFilters(rule, task.RuleType),
            pathFilter: FilterUtil.GetPathFilters(task.Filter),
            ruleModel: new RuleModel { Filter = task.Filter, Rule = task.TaskRule, RuleType = task.RuleType })
        { RuleName = task.TaskRule, Language = language, AIServiceLlm = llm, Prompt = task.UserDefinePromptsJson, Argument = task.Argument };

        Logger.Info($"开始定时执行 SourcePath: {operationParameters.SourcePath}, TargetPath: {operationParameters.TargetPath}");
        // 启动独立的线程来执行操作，避免参数冲突
        await Task.Run(async () =>
        {
            await OperationHandler.ExecuteOperationAsync(task.OperationMode, operationParameters);
            TrayIconService.SetStatus(TrayIconStatus.Normal);
        });
    }

    // Helper method to retrieve task based on task ID or job name
    private async Task<TaskOrchestrationTable> GetTaskAsync(string taskId, IJobExecutionContext context)
    {
        if (!string.IsNullOrEmpty(taskId) && int.TryParse(taskId, out int parsedTaskId))
        {
            Logger.Info($"Retrieving task with ID: {parsedTaskId}");
            return await _dbContext.TaskOrchestration
                .Include(t => t.GroupName)
                .Include(t => t.Filter)
                .FirstOrDefaultAsync(t => t.ID == parsedTaskId && t.IsEnabled == true && t.IsRelated == true);
        }

        string jobName = context.JobDetail.Key.Name;
        if (string.IsNullOrEmpty(jobName))
        {
            Logger.Error("JobName is null");
            return null;
        }

        var idPart = jobName.Split('#').LastOrDefault();
        if (int.TryParse(idPart, out int parsedId))
        {
            return await _dbContext.TaskOrchestration
                .Include(t => t.GroupName)
                .Include(t => t.Filter)
                .FirstOrDefaultAsync(t => t.ID == parsedId && t.IsEnabled == true);
        }

        Logger.Error($"Failed to parse ID from JobName: {jobName}");
        return null;
    }

    /// <summary>
    /// 判断分组是否存在"#"或者"##"规则
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="taskRule"></param>
    /// <returns></returns>
    public async Task<string> GetSpecialCasesRule(int groupId, string taskRule)
    {
        if (!string.IsNullOrEmpty(taskRule) && (taskRule.Trim().Equals("#") || taskRule.Trim().Equals("##")))
        {
            var list = await _dbContext.TaskOrchestration.Where(t => t.GroupName.Id == groupId && t.TaskRule != taskRule).ToListAsync();
            string delimiter = "&";
            return taskRule + string.Join(delimiter, list.Select(x => x.TaskRule));
        }
        return taskRule;
    }

    /// <summary>
    /// 添加定时任务
    /// </summary>
    /// <param name="automaticTable"></param>
    /// <param name="customSchedule"></param>
    /// <returns></returns>
    public static async Task AddTaskConfig(AutomaticTable automaticTable, bool customSchedule = false, IAIServiceLlm llm = null)
    {
        if (automaticTable == null) return;

        // 对任务进行优先级排序，避免简单任务触发时间不一致而优先级不生效
        var taskOrchestrationList = automaticTable.TaskOrchestrationList.OrderByDescending(x => x.Priority).ToList();

        // Handle custom scheduling
        if (customSchedule)
        {
            var cronExpression = !string.IsNullOrEmpty(automaticTable.Schedule.CronExpression) && !automaticTable.IsFileChange
                ? automaticTable.Schedule.CronExpression
                : CronExpressionUtil.GenerateCronExpression(automaticTable, Settings.GeneralConfig.AutomaticRepair);

            foreach (var item in taskOrchestrationList)
            {
                int priority = item.Priority;
                await QuartzHelper.AddJob<AutomaticJob>(
                    $"{item.TaskName}#{item.ID}",
                    item.GroupName.GroupName,
                    cronExpression,
                    priority);
            }
            return;
        }

        // Handle file change monitoring and task scheduling
        var interval = (Convert.ToInt32(string.IsNullOrEmpty(automaticTable.Hourly) ? "0" : automaticTable.Hourly) * 60)
             + Convert.ToInt32(string.IsNullOrEmpty(automaticTable.Minutes) ? "0" : automaticTable.Minutes);
        foreach (var item in taskOrchestrationList)
        {
            var param = new Dictionary<string, object> { { "TaskId", item.ID.ToString() } };
            int priority = item.Priority;
            var ruleModel = new RuleModel
            {
                Rule = item.TaskRule,
                RuleType = item.RuleType,
                Filter = item.Filter
            };

            if (automaticTable.IsFileChange)
            {
                string language = string.IsNullOrEmpty(Settings.Language) ? "Follow the document language" : Settings.Language;
                var delay = Convert.ToInt32(automaticTable.DelaySeconds);
                var sub = Settings.GeneralConfig?.SubFolder ?? false;
                var parameters = new OperationParameters(
                    operationMode: item.OperationMode,
                    sourcePath: item.TaskSource.Equals("DesktopText".GetLocalized())
                    ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    : item.TaskSource,
                    targetPath: item.TaskTarget.Equals("DesktopText".GetLocalized())
                    ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    : item.TaskTarget,
                    fileOperationType: Settings.GeneralConfig.FileOperationType,
                    handleSubfolders: Settings.GeneralConfig.SubFolder ?? false,
                    funcs: [.. FilterUtil.GeneratePathFilters(item.TaskRule, item.RuleType)],
                    pathFilter: FilterUtil.GetPathFilters(item.Filter),
                    new RuleModel() { Filter = item.Filter, Rule = item.TaskRule, RuleType = item.RuleType })
                {
                    Priority = item.Priority,
                    CreateTime = item.CreateTime,
                    AIServiceLlm = llm,
                    Prompt = item.UserDefinePromptsJson,
                    Language = language,
                    Argument = item.Argument
                };
                FileEventHandler.MonitorFolder(parameters, delay);

                if (automaticTable.RegularTaskRunning)
                {
                    await QuartzHelper.AddSimpleJobOfMinuteAsync<AutomaticJob>($"{item.TaskName}#{item.ID}", item.GroupName.GroupName, interval, param, priority);
                }
            }
            else if (automaticTable.RegularTaskRunning)
            {
                await QuartzHelper.AddSimpleJobOfMinuteAsync<AutomaticJob>($"{item.TaskName}#{item.ID}", item.GroupName.GroupName, interval, param, priority);
            }
            // 保证简单定时任务拥有不同的执行起始时间
            await Task.Delay(100);
        }
    }

}
