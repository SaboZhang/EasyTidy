using System.Collections.ObjectModel;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using EasyTidy.Common.Database;
using EasyTidy.Common.Job;
using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using EasyTidy.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Data;

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
            await Task.Run(() =>{
                dispatcherQueue.TryEnqueue(async () => 
                {
                    var list = await _dbContext.TaskOrchestration.Include(x => x.GroupName)
                    .Where(x => string.IsNullOrEmpty(x.TaskSource))
                    .GroupBy(x => x.GroupName)
                    .Select(g => g.First())
                    .ToListAsync();
                    TaskList = new(list);
                    TaskListACV = new AdvancedCollectionView(TaskList, true);
                });
            });
        }catch (Exception ex)
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
    
}
