using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using CommunityToolkit.WinUI.Collections;
using EasyTidy.Common.Database;
using EasyTidy.Common.Extensions;
using EasyTidy.Common.Job;
using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.Service.AIService;
using EasyTidy.Views.ContentDialogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;
using System.Data;

namespace EasyTidy.ViewModels;

public partial class TaskOrchestrationViewModel : ObservableRecipient
{
    private readonly AppDbContext _dbContext;

    private StackedNotificationsBehavior? _notificationQueue;

    private readonly AIServiceFactory _factory;

    [ObservableProperty]
    private IThemeSelectorService _themeSelectorService;

    public TaskOrchestrationViewModel(IThemeSelectorService themeSelectorService, AIServiceFactory factory)
    {
        _themeSelectorService = themeSelectorService;
        _dbContext = App.GetService<AppDbContext>();
        _factory = factory;
        LoadRulesMenu();
        DateTimeModel = new ObservableCollection<PatternSnippetModel>();
        CounterModel = new ObservableCollection<PatternSnippetModel>();
        RandomizerModel = new ObservableCollection<PatternSnippetModel>();
        ReplaceModel = new ObservableCollection<PatternSnippetModel>();
        InitializeRenameModel();
    }

    [ObservableProperty]
    private IList<OperationMode> operationModes = Enum.GetValues(typeof(OperationMode)).Cast<OperationMode>().ToList();

    [ObservableProperty]
    private IList<Encrypted> _encrypteds = Enum.GetValues(typeof(Encrypted)).Cast<Encrypted>().ToList();

    [ObservableProperty]
    private OperationMode _selectedOperationMode;

    [ObservableProperty]
    private string _taskSource = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    [ObservableProperty]
    private string _taskTarget = string.Empty;

    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    [ObservableProperty]
    private ObservableCollection<TaskOrchestrationTable> _taskList;

    [ObservableProperty]
    private List<string> _groupList = [];

    [ObservableProperty]
    private List<string> _groupNameList = [];

    [ObservableProperty]
    private AdvancedCollectionView _taskListACV;

    [ObservableProperty]
    private string _selectedGroupName = string.Empty;

    [ObservableProperty]
    private string _selectedTaskGroupName = string.Empty;

    [ObservableProperty]
    private FilterTable _selectedFilter;

    [ObservableProperty]
    private ObservableCollection<FilterTable> _filterList;

    [ObservableProperty]
    private AdvancedCollectionView _filterListACV;

    [ObservableProperty]
    private int _selectedGroupIndex = -1;

    [ObservableProperty]
    private string _groupTextName = string.Empty;

    public ObservableCollection<MenuCategory> MenuCategories { get; set; }

    [ObservableProperty]
    private ObservableCollection<PatternSnippetModel> _dateTimeModel;

    [ObservableProperty]
    public ObservableCollection<PatternSnippetModel> _counterModel;

    [ObservableProperty]
    public ObservableCollection<PatternSnippetModel> _randomizerModel;

    [ObservableProperty]
    public ObservableCollection<PatternSnippetModel> _replaceModel;

    [ObservableProperty]
    private bool _isExecuting = false;

    private bool? _orederChecked;

    public bool OrederChecked
    {
        get => _orederChecked ?? Settings.IdOrder;
        set
        {
            if (_orederChecked != value)
            {
                _orederChecked = value;
                OnPropertyChanged();
            }
        }
    }

    private static DateTime LastInvocationTime = DateTime.MinValue;

    public void Initialize(StackedNotificationsBehavior notificationQueue)
    {
        _notificationQueue = notificationQueue;
    }

    public void Uninitialize()
    {
        _notificationQueue = null;
    }

    /// <summary>
    /// 添加
    /// </summary>
    /// <param name="sender"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnAddTaskClick(object sender)
    {
        var dialog = new AddTaskContentDialog
        {
            ViewModel = this,
            Title = "AdditionalText".GetLocalized(),
            PrimaryButtonText = "SaveText".GetLocalized(),
            CloseButtonText = "CancelText".GetLocalized(),
            TaskTarget = string.Empty,
            Password = string.IsNullOrEmpty(Settings.EncryptedPassword) 
            ? string.Empty 
            : CryptoUtil.DesDecrypt(Settings.EncryptedPassword),
            Encencrypted = Settings.Encrypted,
            IsSourceFile = Settings.OriginalFile
        };
        SelectedGroupName = string.Empty;
        SelectedGroupIndex = -1;
        dialog.PrimaryButtonClick += OnAddTaskPrimaryButton;

        await dialog.ShowAsync();
    }

    private async void OnAddTaskPrimaryButton(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            var dialog = sender as AddTaskContentDialog;
            if (!dialog.IsValid || string.IsNullOrEmpty(dialog.TaskRule) || string.IsNullOrEmpty(GroupTextName))
            {
                args.Cancel = true;
                return;
            }
            Settings.EncryptedPassword = string.IsNullOrEmpty(Settings.EncryptedPassword) 
            ? CryptoUtil.DesEncrypt(dialog.Password) 
            : Settings.EncryptedPassword;
            Settings.Encrypted = dialog.Encencrypted;
            Settings.OriginalFile = dialog.IsSourceFile;
            await _dbContext.TaskOrchestration.AddAsync(new TaskOrchestrationTable
            {
                TaskName = dialog.TaskName,
                TaskRule = dialog.TaskRule,
                TaskSource = SelectedOperationMode == OperationMode.Delete || SelectedOperationMode == OperationMode.RecycleBin
                ? TaskTarget : ExtractBasePath(TaskTarget, SelectedOperationMode, TaskSource),
                Shortcut = dialog.Shortcut,
                TaskTarget = TaskTarget,
                OperationMode = SelectedOperationMode,
                IsEnabled = dialog.EnabledFlag,
                RuleType = dialog.RuleType,
                CreateTime = DateTime.Now,
                Priority = 1,
                GroupName = !string.IsNullOrEmpty(SelectedTaskGroupName) && SelectedTaskGroupName != "AllText".GetLocalized()
                ? await GetOrUpdateTaskGroupAsync(SelectedTaskGroupName)
                : new TaskGroupTable
                {
                    GroupName = GroupTextName,
                    IsUsed = false
                },
                Filter = SelectedFilter != null
                ? await _dbContext.Filters.Where(x => x.Id == SelectedFilter.Id).FirstOrDefaultAsync() : null,
                IsRegex = dialog.IsRegex
            });
            await _dbContext.SaveChangesAsync();
            if (dialog.Shortcut)
            {
                var result = ShortcutUtil.CreateShortcutDesktop(dialog.TaskName, TaskTarget);
                if (!result)
                {
                    _notificationQueue.ShowWithWindowExtension("CreateShortcutFailedText".GetLocalized(), InfoBarSeverity.Error);
                    _ = ClearNotificationAfterDelay(3000);
                    Logger.Error($"TaskOrchestrationViewModel: OnAddTaskClick 创建桌面快捷方式失败");
                }
            }
            await OnPageLoaded();
            TaskTarget = string.Empty;
            SelectedFilter = null;
            SelectedGroupName = null;
            SelectedGroupIndex = -1;
            GroupTextName = string.Empty;
            _notificationQueue.ShowWithWindowExtension("SaveSuccessfulText".GetLocalized(), InfoBarSeverity.Success);
            _ = ClearNotificationAfterDelay(3000);
        }
        catch (Exception ex)
        {
            _notificationQueue.ShowWithWindowExtension("SaveFailedText".GetLocalized(), InfoBarSeverity.Error);
            _ = ClearNotificationAfterDelay(3000);
            Logger.Error($"TaskOrchestrationViewModel: OnAddTaskClick 异常信息 {ex}");
        }

    }

    /// <summary>
    /// 处理路径
    /// </summary>
    /// <param name="targetPath"></param>
    /// <param name="operation"></param>
    /// <param name="sourcePath"></param>
    /// <returns></returns>
    private string ExtractBasePath(string targetPath, OperationMode operation, string sourcePath)
    {
        // 处理重命名模式
        if (operation == OperationMode.Rename)
        {
            // 获取所有模式代码并替换
            targetPath = RemovePatternsFromPath(targetPath);
            return targetPath.TrimEnd('\\');
        }

        // 处理特殊路径 Desktop
        return IsLocalizedDesktopPath(sourcePath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : sourcePath;
    }

    // 获取所有模式代码并移除
    private string RemovePatternsFromPath(string path)
    {
        var allPatterns = GetAllPatterns();
        foreach (var pattern in allPatterns)
        {
            path = path.Replace(pattern, "");
        }
        return path;
    }

    // 获取 DateTimeModel、CounterModel 和 RandomizerModel 的所有代码模式
    private IEnumerable<string> GetAllPatterns()
    {
        return DateTimeModel.Select(x => x.Code)
            .Concat(CounterModel.Select(x => x.Code))
            .Concat(RandomizerModel.Select(x => x.Code))
            .Concat(ReplaceModel.Select(x => x.Code));
    }

    // 判断 sourcePath 是否为桌面路径
    private bool IsLocalizedDesktopPath(string path)
    {
        return path.Equals("DesktopText".GetLocalized(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 获取或更新任务组
    /// </summary>
    /// <param name="groupName"></param>
    /// <returns></returns>
    private async Task<TaskGroupTable> GetOrUpdateTaskGroupAsync(string groupName)
    {
        var taskGroup = await _dbContext.TaskGroup
            .FirstOrDefaultAsync(x => x.GroupName == groupName);

        if (taskGroup != null && taskGroup.IsUsed)
        {
            taskGroup.IsUsed = false;
        }
        _dbContext.TaskGroup.Update(taskGroup);
        return taskGroup;
    }

    /// <summary>
    /// 选择源文件
    /// </summary>
    [RelayCommand]
    private async Task OnSelectSourcePath()
    {
        try
        {
            var folder = await FileAndFolderPickerHelper.PickSingleFolderAsync(App.MainWindow);
            TaskSource = folder?.Path ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        }
        catch (Exception ex)
        {
            TaskSource = "";
            Logger.Error($"TaskOrchestrationViewModel: OnSelectSourcePath 异常信息 {ex}");
        }
    }

    /// <summary>
    /// 选择目标文件
    /// </summary>
    [RelayCommand]
    private async Task OnSelectTargetPath()
    {
        try
        {
            var folder = await FileAndFolderPickerHelper.PickSingleFolderAsync(App.MainWindow);
            TaskTarget = folder?.Path ?? "";

        }
        catch (Exception ex)
        {
            TaskTarget = "";
            Logger.Error($"TaskOrchestrationViewModel: OnSelectTargetPath 异常信息 {ex}");
        }
    }

    /// <summary>
    /// 页面加载
    /// </summary>
    [RelayCommand]
    private async Task OnPageLoaded()
    {
        IsActive = true;

        try
        {
            await Task.Run(() =>
            {
                dispatcherQueue.TryEnqueue(async () =>
                {
                    var list = await _dbContext.TaskOrchestration.Include(x => x.GroupName).ToListAsync();
                    var filterList = await _dbContext.Filters.ToListAsync();
                    foreach (var item in list)
                    {
                        if (item.TaskSource == Environment.GetFolderPath(Environment.SpecialFolder.Desktop))
                        {
                            item.TaskSource = "DesktopText".GetLocalized();
                        }
                        item.TagOrder = Settings.IdOrder;
                    }
                    GroupList = new(list.Select(x => x.GroupName.GroupName).Distinct().ToList());
                    var newList = list.Select(x => x.GroupName.GroupName).Distinct().ToList();
                    newList.Insert(0, "AllText".GetLocalized());
                    GroupNameList = new(newList);
                    FilterList = new(filterList);
                    FilterListACV = new AdvancedCollectionView(FilterList, true);
                    FilterListACV.SortDescriptions.Add(new SortDescription("Id", SortDirection.Ascending));
                    TaskList = new(list);
                    TaskListACV = new AdvancedCollectionView(TaskList, true);
                    TaskListACV.SortDescriptions.Add(new SortDescription("Priority", SortDirection.Descending));
                    TaskListACV.SortDescriptions.Add(new SortDescription("ID", SortDirection.Ascending));
                });
            });

        }
        catch (Exception ex)
        {
            IsActive = false;
            Logger.Error($"TaskOrchestrationViewModel: OnPageLoad 异常信息 {ex}");
        }

        IsActive = false;
    }

    /// <summary>
    /// 更新
    /// </summary>
    /// <param name="dataContext"></param>
    [RelayCommand]
    private async Task OnUpdateTask(object dataContext)
    {
        var currentTime = DateTime.Now;
        if ((currentTime - LastInvocationTime).TotalMilliseconds < 500) // 防抖时间间隔，单位为毫秒
        {
            Logger.Info($"触发防抖了，鼠标坏了吧，500毫秒两次点击🤣🤣🤣");
            return;
        }
        LastInvocationTime = currentTime;
        try
        {
            if (dataContext != null)
            {
                var dialog = new AddTaskContentDialog
                {
                    ViewModel = this,
                    Title = "ModifyText".GetLocalized(),
                    PrimaryButtonText = "SaveText".GetLocalized(),
                    CloseButtonText = "CancelText".GetLocalized(),
                };

                var task = dataContext as TaskOrchestrationTable;
                dialog.TaskName = task.TaskName;
                dialog.TaskRule = task.TaskRule;
                TaskSource = task.TaskSource;
                TaskTarget = task.TaskTarget;
                dialog.Shortcut = task.Shortcut;
                SelectedOperationMode = task.OperationMode;
                SelectedTaskGroupName = task.GroupName.GroupName;
                GroupTextName = task.GroupName.GroupName;
                SelectedFilter = task.Filter;
                dialog.EnabledFlag = task.IsEnabled;
                dialog.IsRegex = task.IsRegex;
                dialog.RuleType = task.RuleType;
                dialog.IsSourceFile = Settings.OriginalFile;
                dialog.Password = string.IsNullOrEmpty(Settings.EncryptedPassword) 
                ? string.Empty 
                : CryptoUtil.DesDecrypt(Settings.EncryptedPassword);
                dialog.Encencrypted = Settings.Encrypted;

                dialog.PrimaryButtonClick += async (s, e) =>
                {
                    if (dialog.HasErrors)
                    {
                        e.Cancel = true;
                        return;
                    }
                    var oldTask = await _dbContext.TaskOrchestration.Include(x => x.GroupName).Where(x => x.ID == task.ID).FirstOrDefaultAsync();
                    await UpdateQuartzGroup(oldTask.GroupName.GroupName, $"{oldTask.TaskName}#{oldTask.ID}", GroupTextName, $"{dialog.TaskName}#{oldTask.ID}");
                    oldTask.TaskName = dialog.TaskName;
                    var group = await GroupUpdateOrCreate(GroupTextName);
                    oldTask.TaskRule = dialog.TaskRule;
                    oldTask.TaskSource = TaskSource;
                    oldTask.Shortcut = dialog.Shortcut;
                    oldTask.TaskTarget = TaskTarget;
                    oldTask.OperationMode = SelectedOperationMode;
                    oldTask.IsEnabled = dialog.EnabledFlag;
                    oldTask.GroupName = group;
                    oldTask.IsRegex = dialog.IsRegex;
                    oldTask.RuleType = dialog.RuleType;
                    Settings.EncryptedPassword = dialog.Password;
                    Settings.Encrypted = dialog.Encencrypted;
                    Settings.OriginalFile = dialog.IsSourceFile;
                    oldTask.Filter = SelectedFilter != null 
                    ? await _dbContext.Filters.Where(x => x.Id == SelectedFilter.Id).FirstOrDefaultAsync()
                    : null;
                    await _dbContext.SaveChangesAsync();
                    await OnPageLoaded();
                    _notificationQueue.ShowWithWindowExtension("ModifySuccessfullyText".GetLocalized(), InfoBarSeverity.Success);
                    _ = ClearNotificationAfterDelay(3000);
                };

                await dialog.ShowAsync();

            }

        }
        catch (Exception ex)
        {
            _notificationQueue.ShowWithWindowExtension("ModificationFailedText".GetLocalized(), InfoBarSeverity.Error);
            _ = ClearNotificationAfterDelay(3000);
            Logger.Error($"TaskOrchestrationViewModel: OnUpdateTask 异常信息 {ex}");
        }

    }

    private async Task<TaskGroupTable> GroupUpdateOrCreate(string groupTextName)
    {
        try
        {
            var group = await _dbContext.TaskGroup.FirstOrDefaultAsync(g => g.GroupName == groupTextName);
            if (group == null)
            {
                group = new TaskGroupTable()
                {
                    GroupName = GroupTextName
                };
                await _dbContext.TaskGroup.AddAsync(group);
                await _dbContext.SaveChangesAsync();
            }
            return group;
        }
        catch (Exception ex) 
        { 
            Logger.Error($"TaskOrchestrationViewModel: GroupUpdateOrCreate 异常信息 {ex}");
            return null;
        }
    }

    private async Task UpdateQuartzGroup(string groupName, string taskName, string newGroupName, string newTaskName)
    {
        try
        {
            await QuartzHelper.UpdateJob(taskName, groupName, newTaskName, newGroupName);
        }
        catch (Exception ex) 
        {
            Logger.Error($"TaskOrchestrationViewModel: UpdateQuartzGroup 异常信息 {ex}");
        }
        
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnDeleteTask(object dataContext)
    {
        IsActive = true;
        try
        {
            if (dataContext is not TaskOrchestrationTable task)
            {
                _notificationQueue.ShowWithWindowExtension("InvalidTaskData".GetLocalized(), InfoBarSeverity.Error);
                return;
            }

            // 使用事务确保一致性
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            var delete = await _dbContext.TaskOrchestration
                .Include(x => x.GroupName)
                .Include(x => x.AutomaticTable)
                .FirstOrDefaultAsync(x => x.ID == task.ID);

            if (delete == null)
            {
                _notificationQueue.ShowWithWindowExtension("TaskNotFoundText".GetLocalized(), InfoBarSeverity.Warning);
                return;
            }

            await DeleteAssociatedAutomaticTable(delete.AutomaticTable, delete.ID);
            await DeleteTask(delete);
            await DeleteEmptyGroup(delete.GroupName);

            await transaction.CommitAsync(); // 提交事务

            // 删除Quartz任务和停止文件监控
            await DeleteQuartzJob(task);
            FileEventHandler.StopMonitor(task.TaskSource, task.TaskTarget);

            // 刷新页面并通知用户
            await OnPageLoaded();
            _notificationQueue.ShowWithWindowExtension("DeleteSuccessfulText".GetLocalized(), InfoBarSeverity.Success);
            _ = ClearNotificationAfterDelay(3000);
        }
        catch (Exception ex)
        {
            Logger.Error($"TaskOrchestrationViewModel: OnDeleteTask 异常信息 {ex}");
            _notificationQueue.ShowWithWindowExtension("DeleteFailedText".GetLocalized(), InfoBarSeverity.Error);
            _ = ClearNotificationAfterDelay(3000);
        }
        finally
        {
            IsActive = false;
        }
    }

    private async Task DeleteAssociatedAutomaticTable(AutomaticTable automaticTable, int taskId)
    {
        if (automaticTable == null) return;

        var scheduleId = automaticTable?.Schedule?.ID;
        var schedule = await _dbContext.Schedule
            .FirstOrDefaultAsync(x => x.ID == scheduleId);

        if (schedule != null)
        {
            _dbContext.Schedule.Remove(schedule);
        }

        var isAutomaticTableInUse = await _dbContext.TaskOrchestration
            .AnyAsync(x => x.AutomaticTable.ID == automaticTable.ID && x.ID != taskId);

        if (!isAutomaticTableInUse)
        {
            _dbContext.Automatic.Remove(automaticTable);
        }
    }

    private async Task DeleteTask(TaskOrchestrationTable delete)
    {
        _dbContext.TaskOrchestration.Remove(delete);
        await _dbContext.SaveChangesAsync();
    }

    private async Task DeleteEmptyGroup(TaskGroupTable group)
    {
        if (group == null) return;

        var hasOtherTasks = await _dbContext.TaskOrchestration
            .AnyAsync(x => x.GroupName.Id == group.Id);

        if (!hasOtherTasks)
        {
            _dbContext.TaskGroup.Remove(group);
            await _dbContext.SaveChangesAsync();
        }
    }

    private async Task DeleteQuartzJob(TaskOrchestrationTable task)
    {
        if (task.GroupName == null) return;
        await QuartzHelper.DeleteJob(task.TaskName + "#" + task.ID, task.GroupName.GroupName);
    }

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnExecuteTask(object dataContext)
    {
        var currentTime = DateTime.Now;
        if ((currentTime - LastInvocationTime).TotalMilliseconds < 500) // 防抖时间间隔，单位为毫秒
        {
            Logger.Info($"触发防抖了，鼠标坏了吧，500毫秒两次点击🤣🤣🤣");
            return;
        }
        LastInvocationTime = currentTime;
        IsActive = true;
        try
        {
            if (dataContext != null)
            {
                var task = dataContext as TaskOrchestrationTable;
                var automatic = new AutomaticJob();
                var rule = await automatic.GetSpecialCasesRule(task.GroupName.Id, task.TaskRule);
                var operationParameters = new OperationParameters(
                    operationMode: task.OperationMode,
                    sourcePath: task.TaskSource.Equals("DesktopText".GetLocalized())
                    ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    : task.TaskSource,
                    targetPath: task.TaskTarget,
                    fileOperationType: Settings.GeneralConfig.FileOperationType,
                    handleSubfolders: Settings.GeneralConfig.SubFolder ?? false,
                    funcs: FilterUtil.GeneratePathFilters(rule, task.RuleType),
                    pathFilter: FilterUtil.GetPathFilters(task.Filter),
                    ruleModel: new RuleModel { Filter = task.Filter, Rule = task.TaskRule, RuleType = task.RuleType })
                { RuleName = task.TaskRule };
                await OperationHandler.ExecuteOperationAsync(task.OperationMode, operationParameters);
                _notificationQueue.ShowWithWindowExtension("ExecutionSuccessfulText".GetLocalized(), InfoBarSeverity.Success);
                _ = ClearNotificationAfterDelay(3000);
            }
        }
        catch (Exception ex)
        {
            _notificationQueue.ShowWithWindowExtension("ExecutionFailedText".GetLocalized(), InfoBarSeverity.Error);
            _ = ClearNotificationAfterDelay(3000);
            Logger.Error($"TaskOrchestrationViewModel: OnExecuteTask 异常信息 {ex}");
            IsActive = false;
        }
        IsActive = false;
    }

    /// <summary>
    /// 执行指定的分组
    /// </summary>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnExecuteGroupTask()
    {
        IsActive = true;
        try
        {
            if (IsExecuting)
            {
                var list = await _dbContext.TaskOrchestration.Include(x => x.GroupName)
                    .Where(x => x.GroupName.GroupName == SelectedGroupName).ToListAsync();
                if (list != null)
                {
                    var orderList = list.OrderByDescending(x => x.Priority);
                    foreach (var item in orderList)
                    {
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
                        { RuleName = item.TaskRule };
                        await OperationHandler.ExecuteOperationAsync(item.OperationMode, operationParameters);
                        _notificationQueue.ShowWithWindowExtension("ExecutionSuccessfulText".GetLocalized(), InfoBarSeverity.Success);
                        _ = ClearNotificationAfterDelay(3000);
                        // AI执行
                        var ai = _factory.GetService(ServiceType.OpenAI);
                    }
                }
            }
            IsActive = false;
        }
        catch(Exception ex)
        {
            _notificationQueue.ShowWithWindowExtension("ExecutionFailedText".GetLocalized(), InfoBarSeverity.Error);
            _ = ClearNotificationAfterDelay(3000);
            Logger.Error($"TaskOrchestrationViewModel: OnExecuteGroupTask 异常信息 {ex}");
            IsActive = false;
        }
    }

    /// <summary>
    /// 禁用/启用
    /// </summary>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnIsEnableTask(object dataContext)
    {
        var currentTime = DateTime.Now;
        if ((currentTime - LastInvocationTime).TotalMilliseconds < 500) // 防抖时间间隔，单位为毫秒
        {
            Logger.Info($"触发防抖了，鼠标坏了吧，500毫秒两次点击🤣🤣🤣");
            return;
        }
        LastInvocationTime = currentTime;
        IsActive = true;
        try
        {
            if (dataContext != null)
            {
                var task = dataContext as TaskOrchestrationTable;
                var update = await _dbContext.TaskOrchestration.Where(x => x.ID == task.ID).FirstOrDefaultAsync();
                update.IsEnabled = !update.IsEnabled;
                await _dbContext.SaveChangesAsync();
                await (update.IsEnabled
                    ? QuartzHelper.ResumeJob(task.TaskName + "#" + task.ID.ToString(), task.GroupName.GroupName)
                    : QuartzHelper.PauseJob(task.TaskName + "#" + task.ID.ToString(), task.GroupName.GroupName));
                await OnPageLoaded();
                _notificationQueue.ShowWithWindowExtension("DisabledSuccessfullyText".GetLocalized(), InfoBarSeverity.Success);
                _ = ClearNotificationAfterDelay(3000);
            }
        }
        catch (Exception ex)
        {
            _notificationQueue.ShowWithWindowExtension("DisablingFailedText".GetLocalized(), InfoBarSeverity.Error);
            _ = ClearNotificationAfterDelay(3000);
            Logger.Error($"FileExplorerViewModel: OnIsEnableTask 异常信息 {ex}");
            IsActive = false;
        }
        IsActive = false;

    }

    /// <summary>
    /// 分组
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnGroupNameChanged()
    {
        IsActive = true;
        IsExecuting = false;
        try
        {
            var list = await Task.Run(async () =>
            {
                var query = SelectedGroupName == "AllText".GetLocalized()
                ? _dbContext.TaskOrchestration.Include(x => x.GroupName)
                : _dbContext.TaskOrchestration.Include(x => x.GroupName).Where(x => x.GroupName.GroupName == SelectedGroupName);

                var resultList = await query.ToListAsync();

                foreach (var item in resultList)
                {
                    if (item.TaskSource == Environment.GetFolderPath(Environment.SpecialFolder.Desktop))
                    {
                        item.TaskSource = "DesktopText".GetLocalized();
                    }
                }

                return resultList;
            });

            if (!string.IsNullOrEmpty(SelectedGroupName) && !SelectedGroupName.Equals("AllText".GetLocalized()) && list.Count != 0)
            {
                IsExecuting = true;
            }

            TaskList = new(list);
            TaskListACV = new AdvancedCollectionView(TaskList, true);
            TaskListACV.SortDescriptions.Add(new SortDescription("Priority", SortDirection.Descending));
            TaskListACV.SortDescriptions.Add(new SortDescription("ID", SortDirection.Ascending));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}");
            IsActive = false;
        }
        IsActive = false;
    }

    [RelayCommand]
    private Task OnSelectedItemChanged(object dataContext)
    {
        try
        {
            if (dataContext != null)
            {
                var listViews = dataContext as TeachingTip;
                var listView = listViews.HeroContent as ListView;
                var item = listView.SelectedItem;
                if (item is FilterTable filter)
                {
                    SelectedFilter = filter;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"TaskOrchestrationViewModel: OnSelectedItemChanged 异常信息 {ex}");
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void OnGroupSelectionChanged()
    {
        if (string.IsNullOrEmpty(SelectedTaskGroupName)
            || "AllText".GetLocalized().Equals(SelectedTaskGroupName))
        {
            SelectedTaskGroupName = string.Empty;
        }
        else
        {
            GroupTextName = SelectedTaskGroupName;
        }
    }

    public async Task OnTaskListCollectionChangedAsync(TaskOrchestrationTable task, int draggedIndex, int droppedIndex)
    {
        if ((DateTime.Now - LastInvocationTime).TotalMilliseconds < 800)
        {
            return; // 防止频繁触发
        }
        LastInvocationTime = DateTime.Now;

        if (draggedIndex < 0 || droppedIndex < 0 || draggedIndex == droppedIndex || draggedIndex >= TaskList.Count || droppedIndex >= TaskList.Count)
        {
            return;
        }
        TaskList.Move(draggedIndex, droppedIndex);

        for (int i = 0; i < TaskList.Count; i++)
        {
            TaskList[i].Priority = TaskList.Count - 1 - i;

            if (task.ID == TaskList[i].ID)
            {
                await QuartzHelper.UpdateTaskPriority($"{task.TaskName}#{task.ID}", task.GroupName.GroupName, TaskList[i].Priority);
            }
        }

        _dbContext.TaskOrchestration.UpdateRange(TaskList);
        await _dbContext.SaveChangesAsync();
        TaskListACV = new AdvancedCollectionView(TaskList, true);
        TaskListACV.SortDescriptions.Add(new SortDescription("Priority", SortDirection.Descending));
        TaskListACV.SortDescriptions.Add(new SortDescription("ID", SortDirection.Ascending));
    }

    /// <summary>
    /// 加载规则菜单
    /// </summary>
    private void LoadRulesMenu()
    {
        var orderedFolderRules = FolderRulesMenuDescription.ToList(); // 转为 List 保留顺序
        var orderedFileRules = FileRulesMenuDescription.ToList();
        MenuCategories =
        [
            new() {
                Title = "HandlingRulesForFiles".GetLocalized(),
                Items = orderedFileRules.Select(kv => kv.Value + " = " + kv.Key.GetLocalized()).ToList()
            },
            new() {
                Title = "HandlingFolderRules".GetLocalized(),
                Items = orderedFolderRules.Select(kv => kv.Value + " = " + kv.Key.GetLocalized()).ToList()
            }
        ];
    }

    private readonly Dictionary<string, string> FolderRulesMenuDescription = new Dictionary<string, string>
    {
        { "AllFoldersText","**" },
        { "AllFoldersWithoutOtherMatches","##" },
        { "AllFoldersStartingWithrobot","robot**" },
        { "AllFoldersContainingrobot","**robot**" },
        { "Use_Or_ToSeparate","image**|photo**" },
        { "UseToExcludeFoldersStartingWithmy","**/my**" },
    };

    private readonly Dictionary<string, string> FileRulesMenuDescription = new Dictionary<string, string>
    {
        { "AllFiles","*" },
        { "AllFilesWithNoOtherMatches","#" },
        { "AllFilesWithTheExtension","*.jpg" },
        { "AllFilesBeginningWith","penguin*" },
        { "AllFilesContains","*penguin*" },
        { "UseDelimiter","*.jpg;*.png" },
        { "UseDelimitersToExclude","*.jpg/sea*" },
        { "CompressedFile","*.7z;*.bz2;*.gz;*.iso;*.rar;*.xz;*.z;*.zip" },
        { "Document","*.djvu;*.doc;*.docx;*.epub;*.odt;*.pdf;*.rtp;*.tex;*.txt" },
        { "Photograph","*.bmp;*.gif;*.ico;*.jpg;*.jpeg;*.png;*.psd;*.tif;*.tiff" },
        { "Music","*.aac;*.flac;*.m4a;*mp3*.ogg;*.wma;*.wav" },
        { "PresentationAndWorksheet","*.csv;*.odp;*.ods;*.pps;*.ppt;*.pptx;*.xls;*.xlsx" },
        { "Video","*.avi;*.fly;*.m4v;*.mkv;*.mov;*.mp4;*.mpeg;*.mpg;*.wmv" },
    };

    private void InitializeRenameModel()
    {
        // 初始化日期时间方式
        DateTimeModel.Add(new PatternSnippetModel("$YYYY", "DateTimeCheatSheet_FullYear".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$YY", "DateTimeCheatSheet_YearLastTwoDigits".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$Y", "DateTimeCheatSheet_YearLastDigit".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$MMMM", "DateTimeCheatSheet_MonthName".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$MMM", "DateTimeCheatSheet_MonthNameAbbr".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$MM", "DateTimeCheatSheet_MonthDigitLZero".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$M", "DateTimeCheatSheet_MonthDigit".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$DDDD", "DateTimeCheatSheet_DayName".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$DDD", "DateTimeCheatSheet_DayNameAbbr".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$DD", "DateTimeCheatSheet_DayDigitLZero".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$D", "DateTimeCheatSheet_DayDigit".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$hh", "DateTimeCheatSheet_HoursLZero".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$h", "DateTimeCheatSheet_Hours".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$mm", "DateTimeCheatSheet_MinutesLZero".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$m", "DateTimeCheatSheet_Minutes".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$ss", "DateTimeCheatSheet_SecondsLZero".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$s", "DateTimeCheatSheet_Seconds".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$fff", "DateTimeCheatSheet_MilliSeconds3D".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$ff", "DateTimeCheatSheet_MilliSeconds2D".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$f", "DateTimeCheatSheet_MilliSeconds1D".GetLocalized()));

        // 初始化计数器方式
        CounterModel.Add(new PatternSnippetModel("${}", "CounterCheatSheet_Simple".GetLocalized()));
        CounterModel.Add(new PatternSnippetModel("${start=10}", "CounterCheatSheet_Start".GetLocalized()));
        CounterModel.Add(new PatternSnippetModel("${increment=5}", "CounterCheatSheet_Increment".GetLocalized()));
        CounterModel.Add(new PatternSnippetModel("${padding=8}", "CounterCheatSheet_Padding".GetLocalized()));
        CounterModel.Add(new PatternSnippetModel("${increment=3,padding=4,start=900}", "CounterCheatSheet_Complex".GetLocalized()));

        // 初始化随机化方式
        RandomizerModel.Add(new PatternSnippetModel("${rstringalnum=9}", "RandomizerCheatSheet_Alnum".GetLocalized()));
        RandomizerModel.Add(new PatternSnippetModel("${rstringalpha=13}", "RandomizerCheatSheet_Alpha".GetLocalized()));
        RandomizerModel.Add(new PatternSnippetModel("${rstringdigit=36}", "RandomizerCheatSheet_Digit".GetLocalized()));
        RandomizerModel.Add(new PatternSnippetModel("${ruuidv4}", "RandomizerCheatSheet_Uuid".GetLocalized()));

        // 初始化替换方式
        ReplaceModel.Add(new PatternSnippetModel("${source}", "ReplaceCheatSheet_ReplaceSource".GetLocalized()));
        ReplaceModel.Add(new PatternSnippetModel("${parent}", "ReplaceCheatSheet_ReplaceParent".GetLocalized()));
        ReplaceModel.Add(new PatternSnippetModel("${replace=old,new,false}", "ReplaceCheatSheet_Replace".GetLocalized()));
        ReplaceModel.Add(new PatternSnippetModel("${replace=old,new,true}", "ReplaceCheatSheet_ReplaceIgnoreCase".GetLocalized()));
        ReplaceModel.Add(new PatternSnippetModel("${replace=old,,false}", "ReplaceCheatSheet_ReplaceDelete".GetLocalized()));
        ReplaceModel.Add(new PatternSnippetModel("${regex=^foo,new}", "ReplaceCheatSheet_ReplaceRegex".GetLocalized()));
        ReplaceModel.Add(new PatternSnippetModel("${regex=^foo,}", "ReplaceCheatSheet_ReplaceRegexDelete".GetLocalized()));
    }

    private async Task ClearNotificationAfterDelay(int delayMilliseconds)
    {
        await Task.Delay(delayMilliseconds);  // 延迟指定的毫秒数
        _notificationQueue?.Clear();  // 清除通知
    }

}

