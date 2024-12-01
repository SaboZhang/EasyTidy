using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using CommunityToolkit.WinUI.Collections;
using EasyTidy.Common.Database;
using EasyTidy.Common.Extensions;
using EasyTidy.Common.Job;
using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.Util;
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

    [ObservableProperty]
    private IThemeSelectorService _themeSelectorService;

    public TaskOrchestrationViewModel(IThemeSelectorService themeSelectorService)
    {
        _themeSelectorService = themeSelectorService;
        _dbContext = App.GetService<AppDbContext>();
        LoadRulesMenu();
        DateTimeModel = new ObservableCollection<PatternSnippetModel>();
        CounterModel = new ObservableCollection<PatternSnippetModel>();
        RandomizerModel = new ObservableCollection<PatternSnippetModel>();
        InitializeRenameModel();
    }

    [ObservableProperty]
    private IList<OperationMode> operationModes = Enum.GetValues(typeof(OperationMode)).Cast<OperationMode>().ToList();

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
            TaskTarget = string.Empty
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
            if (dialog.HasErrors)
            {
                args.Cancel = true;
                return;
            }
            int newPriority = dialog.Priority - 5;
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
                Priority = newPriority < 0 ? 5 : newPriority,
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
            .Concat(RandomizerModel.Select(x => x.Code));
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
                dialog.TaskSource = task.TaskSource;
                TaskTarget = task.TaskTarget;
                dialog.Shortcut = task.Shortcut;
                SelectedOperationMode = task.OperationMode;
                SelectedTaskGroupName = task.GroupName.GroupName;
                GroupTextName = task.GroupName.GroupName;
                SelectedFilter = task.Filter;
                dialog.EnabledFlag = task.IsEnabled;
                dialog.IsRegex = task.IsRegex;
                dialog.RuleType = task.RuleType;

                dialog.PrimaryButtonClick += async (s, e) =>
                {
                    if (dialog.HasErrors)
                    {
                        e.Cancel = true;
                        return;
                    }
                    var oldTask = await _dbContext.TaskOrchestration.Include(x => x.GroupName).Where(x => x.ID == task.ID).FirstOrDefaultAsync();
                    oldTask.TaskName = dialog.TaskName;
                    oldTask.TaskRule = dialog.TaskRule;
                    oldTask.TaskSource = TaskSource;
                    oldTask.Shortcut = dialog.Shortcut;
                    oldTask.TaskTarget = TaskTarget;
                    oldTask.OperationMode = SelectedOperationMode;
                    oldTask.IsEnabled = dialog.EnabledFlag;
                    oldTask.GroupName.GroupName = GroupTextName;
                    oldTask.IsRegex = dialog.IsRegex;
                    oldTask.Filter = await _dbContext.Filters.Where(x => x.Id == SelectedFilter.Id).FirstOrDefaultAsync();
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
            _notificationQueue.ShowWithWindowExtension("ModificationFailedText".GetLocalized(), InfoBarSeverity.Success);
            _ = ClearNotificationAfterDelay(3000);
            Logger.Error($"TaskOrchestrationViewModel: OnUpdateTask 异常信息 {ex}");
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
            if (dataContext != null)
            {
                var task = dataContext as TaskOrchestrationTable;
                var delete = await _dbContext.TaskOrchestration.Where(x => x.ID == task.ID).FirstOrDefaultAsync();
                if (delete != null)
                {
                    _dbContext.TaskOrchestration.Remove(delete);
                }
                await _dbContext.SaveChangesAsync();
                await QuartzHelper.DeleteJob(task.TaskName + "#" + task.ID.ToString(), task.GroupName.GroupName);
                FileEventHandler.StopMonitor(task.TaskSource, task.TaskTarget);
                await OnPageLoaded();
                _notificationQueue.ShowWithWindowExtension("DeleteSuccessfulText".GetLocalized(), InfoBarSeverity.Success);
                _ = ClearNotificationAfterDelay(3000);
            }
        }
        catch (Exception ex)
        {
            _notificationQueue.ShowWithWindowExtension("DeleteFailedText".GetLocalized(), InfoBarSeverity.Success);
            _ = ClearNotificationAfterDelay(3000);
            Logger.Error($"TaskOrchestrationViewModel: OnDeleteTask 异常信息 {ex}");
            IsActive = false;
        }
        IsActive = false;
    }

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnExecuteTask(object dataContext)
    {
        IsActive = true;
        try
        {
            if (dataContext != null)
            {
                var task = dataContext as TaskOrchestrationTable;
                var list = await _dbContext.TaskOrchestration.Include(x => x.GroupName).ToListAsync();
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
    /// 禁用/启用
    /// </summary>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnIsEnableTask(object dataContext)
    {
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

            TaskList = new(list);
            TaskListACV = new AdvancedCollectionView(TaskList, true);
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
        { "AllFoldersNamed_robot_LocatedUnder","C:\\**\\robot" },
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
        { "All_FilesUnder","C:\\Folder\\*.jpg" },
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
    }

    private async Task ClearNotificationAfterDelay(int delayMilliseconds)
    {
        await Task.Delay(delayMilliseconds);  // 延迟指定的毫秒数
        _notificationQueue?.Clear();  // 清除通知
    }

}

