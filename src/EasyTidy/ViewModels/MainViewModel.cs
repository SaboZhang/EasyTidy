using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using EasyTidy.Common.Database;
using EasyTidy.Common.Job;
using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.Service.AIService;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Dispatching;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace EasyTidy.ViewModels;
public partial class MainViewModel : ObservableObject
{
    private readonly AppDbContext _dbContext;

    [ObservableProperty]
    private bool isBackEnabled;

    [ObservableProperty]
    private object? selected;

    [ObservableProperty]
    private IThemeSelectorService _themeSelectorService;

    [ObservableProperty]
    private ObservableCollection<TaskOrchestrationTable> _taskList;

    [ObservableProperty]
    private AdvancedCollectionView _taskListACV;

    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public INavigationService NavigationService
    {
        get;
    }

    public INavigationViewService NavigationViewService
    {
        get;
    }
    public MainViewModel()
    {
        _dbContext = App.GetService<AppDbContext>();
        _themeSelectorService = App.GetService<IThemeSelectorService>();
    }

    [RelayCommand]
    private async Task OnPageLoaded()
    {
        try
        {
            await Task.Run(() =>
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                dispatcherQueue.TryEnqueue(async () =>
                {
                    var list = await _dbContext.TaskOrchestration.Include(x => x.GroupName)
                    .Where(x => string.IsNullOrEmpty(x.TaskSource) 
                    || x.TaskSource == "DesktopText".GetLocalized() 
                    || x.TaskSource.Equals(desktopPath))
                    .GroupBy(x => x.GroupName)
                    .Select(g => g.First())
                    .ToListAsync();
                    TaskList = new(list);
                    TaskListACV = new AdvancedCollectionView(TaskList, true);
                });
            });
        }
        catch (Exception ex)
        {
            Logger.Error(ex.Message);
        }
    }

    public async Task<string> ExecuteTaskAsync(TaskOrchestrationTable task, string sourcePath)
    {
        var taskList = await GetTasksByGroupIdAsync(task.GroupName.Id);
        if (taskList.Count == 0) return string.Empty;

        var automatic = new AutomaticJob();

        // 预先计算所有任务的规则，避免在循环中重复调用
        var taskRules = await Task.WhenAll(taskList.Select(async item =>
        {
            var rules = await automatic.GetSpecialCasesRule(item.GroupName.Id, item.TaskRule);
            return new
            {
                TaskItem = item,
                Filters = FilterUtil.GeneratePathFilters(rules, item.RuleType)
            };
        }));

        // **找到第一个符合规则的任务**
        var matchedTask = taskRules.FirstOrDefault(t => !FilterUtil.ShouldSkip(t.Filters, sourcePath, null));

        if (matchedTask == null)
        {
            Logger.Warn($"文件 {sourcePath} 无匹配规则，跳过处理");
            var fileName = Path.GetFileName(sourcePath);
            return fileName; // 没有匹配的任务，直接返回
        }

        // **执行第一个匹配的任务**
        var operationParams = CreateOperationParameters(matchedTask.TaskItem, sourcePath, matchedTask.Filters);
        await OperationHandler.ExecuteOperationAsync(matchedTask.TaskItem.OperationMode, operationParams);
        return string.Empty;
    }

    // 获取任务列表
    private async Task<List<TaskOrchestrationTable>> GetTasksByGroupIdAsync(int groupId)
    {
        return await _dbContext.TaskOrchestration
            .Include(x => x.GroupName)
            .Where(g => g.GroupName.Id == groupId && string.IsNullOrEmpty(g.TaskSource))
            .ToListAsync();
    }

    // 创建文件操作参数
    private OperationParameters CreateOperationParameters(TaskOrchestrationTable item, string sourcePath, List<Func<string, bool>> filters)
    {
        string resolvedSourcePath = sourcePath.Equals("DesktopText".GetLocalized())
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : sourcePath;

        var newTargetPath = Path.Combine(item.TaskTarget, Path.GetFileName(resolvedSourcePath));
        return new OperationParameters(
            operationMode: item.OperationMode,
            sourcePath: resolvedSourcePath,
            targetPath: newTargetPath,
            fileOperationType: Settings.GeneralConfig.FileOperationType,
            handleSubfolders: Settings.GeneralConfig.SubFolder ?? false,
            funcs: filters,
            pathFilter: FilterUtil.GetPathFilters(item.Filter),
            ruleModel: new RuleModel
            {
                Filter = item.Filter,
                Rule = item.TaskRule,
                RuleType = item.RuleType
            })
        { RuleName = item.TaskRule };
    }

    public async Task ExecuteTaskAsync(string path)
    {
        var group = await _dbContext.TaskGroup.Where(x => x.IsDefault == true).FirstOrDefaultAsync();
        if (group == null) return;
        var taskList = await GetTasksByGroupIdAsync(group.Id);
        if (taskList.Count == 0) return;

        var automatic = new AutomaticJob();

        // 预先计算所有任务的规则，避免在循环中重复调用
        var taskRules = await Task.WhenAll(taskList.Select(async item =>
        {
            var rules = await automatic.GetSpecialCasesRule(item.GroupName.Id, item.TaskRule);
            return new
            {
                TaskItem = item,
                Rules = FilterUtil.GeneratePathFilters(rules, item.RuleType),
                Filter = FilterUtil.GetPathFilters(item.Filter)
            };
        }));

        // **找到第一个符合规则的任务**
        var matchedTask = taskRules.FirstOrDefault(t => !FilterUtil.ShouldSkip(t.Rules, path, t.Filter));

        if (matchedTask == null)
        {
            Logger.Warn($"文件 {path} 无匹配规则，跳过处理");
            var fileName = Path.GetFileName(path);
            return; // 没有匹配的任务，直接返回
        }

        // **执行第一个匹配的任务**
        var operationParams = CreateOperationParameters(matchedTask.TaskItem, path, matchedTask.Rules);
        await OperationHandler.ExecuteOperationAsync(matchedTask.TaskItem.OperationMode, operationParams);
        Logger.Debug($"执行任务: {path}");
    }

    public async Task ExecuteAllTaskAsync()
    {
        var taskList = await _dbContext.TaskOrchestration
            .Include(x => x.GroupName)
            .Include(x => x.Filter)
            .Include(x => x.AutomaticTable)
            .Where(x => x.AutomaticTable.Schedule == null).ToListAsync();
        if (taskList.Count == 0) return;
        var orderList = taskList.OrderByDescending(x => x.Priority);
        string language = string.IsNullOrEmpty(Settings.Language) ? "Follow the document language" : Settings.Language;
        foreach (var item in orderList)
        {
            var ai = await _dbContext.AIService.Where(x => x.Identify.ToString().ToLower().Equals(item.AIIdentify.ToString().ToLower())).FirstOrDefaultAsync();
            IAIServiceLlm llm = null;
            if (item.OperationMode == OperationMode.AIClassification || item.OperationMode == OperationMode.AISummary)
            {
                llm = AIServiceFactory.CreateAIServiceLlm(ai, item.UserDefinePromptsJson);
            }
            var automatic = new AutomaticJob();
            var rule = await automatic.GetSpecialCasesRule(item.GroupName.Id, item.TaskRule);
            var operationParameters = new OperationParameters(
                operationMode: item.OperationMode,
                sourcePath: item.TaskSource.Equals("DesktopText".GetLocalized())
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : item.TaskSource,
                targetPath: item.TaskTarget,
                fileOperationType: Settings.GeneralConfig.FileOperationType,
                handleSubfolders: Settings.GeneralConfig.SubFolder ?? false,
                funcs: FilterUtil.GeneratePathFilters(rule, item.RuleType),
                pathFilter: FilterUtil.GetPathFilters(item.Filter),
                ruleModel: new RuleModel { Filter = item.Filter, Rule = item.TaskRule, RuleType = item.RuleType })
            { RuleName = item.TaskRule, AIServiceLlm = llm, Prompt = item.UserDefinePromptsJson, Argument = item.Argument, Language = language };
            await OperationHandler.ExecuteOperationAsync(item.OperationMode, operationParameters);
        }
    }
}
