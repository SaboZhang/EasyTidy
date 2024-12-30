using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using CommunityToolkit.WinUI.Collections;
using EasyTidy.Common.Database;
using EasyTidy.Common.Extensions;
using EasyTidy.Common.Job;
using EasyTidy.Common.Model;
using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.Views.ContentDialogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace EasyTidy.ViewModels;

public partial class AutomaticViewModel : ObservableRecipient
{
    private readonly AppDbContext _dbContext;
    private StackedNotificationsBehavior? _notificationQueue;

    [ObservableProperty]
    private IThemeSelectorService _themeSelectorService;

    public AutomaticViewModel(IThemeSelectorService themeSelectorService)
    {
        _themeSelectorService = themeSelectorService;
        _dbContext = App.GetService<AppDbContext>();
        _taskItems = new ObservableCollection<TaskItem>();
        _automaticModel = new ObservableCollection<AutomaticModel>();
    }

    /// <summary>
    ///     当前配置实例
    /// </summary>
    private static AutomaticConfigModel? CurConfig = Settings.AutomaticConfig;


    /// <summary>
    /// 文件更改时
    /// </summary>
    private bool? _isFileChange;

    public bool IsFileChange
    {
        get => _isFileChange ?? CurConfig.IsFileChange;

        set
        {
            if (_isFileChange != value)
            {

                _isFileChange = value;
                CurConfig.IsFileChange = value;
                NotifyPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 软件启动时执行
    /// </summary>
    private bool? _isStartupExecution;

    public bool IsStartupExecution
    {
        get => _isStartupExecution ?? CurConfig.IsStartupExecution;
        set
        {
            if (_isStartupExecution != value)
            {

                _isStartupExecution = value;
                CurConfig.IsStartupExecution = value;
                NotifyPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 周期执行
    /// </summary>
    private bool? _regularTaskRunning;

    public bool RegularTaskRunning
    {
        get => _regularTaskRunning ?? CurConfig.RegularTaskRunning;
        set
        {
            if (_regularTaskRunning != value)
            {
                _regularTaskRunning = value;
                CurConfig.RegularTaskRunning = value;
                NotifyPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 按计划执行
    /// </summary>
    private bool? _onScheduleExecution;

    public bool OnScheduleExecution
    {

        get => _onScheduleExecution ?? CurConfig.OnScheduleExecution;
        set
        {
            if (_onScheduleExecution != value)
            {
                _onScheduleExecution = value;
                CurConfig.OnScheduleExecution = value;
                NotifyPropertyChanged();
            }
        }
    }

    [ObservableProperty]
    public bool _customFileChange = false;

    [ObservableProperty]
    public bool _customStartupExecution = false;

    [ObservableProperty]
    public bool _customRegularTaskRunning = false;

    [ObservableProperty]
    public bool _customOnScheduleExecution = false;

    [ObservableProperty]
    public AdvancedCollectionView _automaticModelACV;

    [ObservableProperty]
    public AdvancedCollectionView _taskItemsACV;

    [ObservableProperty]
    private ObservableCollection<TaskItem> _taskItems;

    [ObservableProperty]
    private ObservableCollection<AutomaticModel> _automaticModel;

    [ObservableProperty]
    private ObservableCollection<FileListModel> _fileLists;

    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    [ObservableProperty]
    private string _delaySeconds = "5";

    public object ReceivedParameter { get; set; }

    [ObservableProperty]
    private string _selectTaskTime = DateTime.Now.ToString("HH:mm");

    private List<string> _fireTimes;

    private bool _customSchedule = false;

    public bool CustomSchedule
    {
        get => _customSchedule;
        set
        {
            if (_customSchedule != value)
            {
                _customSchedule = value;
                OnPropertyChanged();
            }
        }
    }

    public List<string> FireTimes
    {
        get => _fireTimes;
        set
        {
            if (_fireTimes != value)
            {
                _fireTimes = value;
                OnPropertyChanged();
            }
        }
    }

    public void Initialize(StackedNotificationsBehavior notificationQueue)
    {
        _notificationQueue = notificationQueue;
    }

    public void Uninitialize()
    {
        _notificationQueue = null;
    }

    /// <summary>
    /// 页面加载
    /// </summary>
    /// <returns></returns>
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
                    var list = await _dbContext.TaskOrchestration.Include(x => x.GroupName).Include(x => x.AutomaticTable).ToListAsync();
                    var groupTask = ProcessTaskGrouping(list);
                    var autoList = ProcessAutomaticList(list);
                    TaskItems = new ObservableCollection<TaskItem>(groupTask);
                    AutomaticModel = new(autoList);
                    TaskItemsACV = new AdvancedCollectionView(TaskItems, true);
                    TaskItemsACV.SortDescriptions.Add(new SortDescription("Id", SortDirection.Ascending));
                    AutomaticModelACV = new AdvancedCollectionView(AutomaticModel, true);
                    AutomaticModelACV.SortDescriptions.Add(new SortDescription("Id", SortDirection.Ascending));

                });
            });

        }
        catch (Exception ex)
        {
            IsActive = false;
            Logger.Error($"AutomaticViewModel: OnPageLoad 异常信息 {ex}");
        }

        IsActive = false;
    }

    /// <summary>
    /// 加载任务组数据
    /// </summary>
    /// <param name="taskData"></param>
    /// <returns></returns>
    private static List<TaskItem> ProcessTaskGrouping(List<TaskOrchestrationTable> taskData)
    {
        var isExpanded = taskData.Count <= 5;

        return taskData
            .GroupBy(x => x.GroupName.GroupName)
            .Select(g =>
            {
                var groupName = g.Key;
                var firstItem = g.FirstOrDefault()?.GroupName;
                var parentItem = new TaskItem(null)
                {
                    Name = groupName,
                    IsExpanded = isExpanded,
                    Id = firstItem?.Id ?? 0
                };

                var childItems = g.Select(x => new TaskItem(parentItem)
                {
                    Name = x.TaskName,
                    IsSelected = x.IsRelated,
                    Id = x.ID
                }).ToList();

                childItems.ForEach(childItem => parentItem.Children.Add(childItem));
                parentItem.UpdateIsSelected();

                parentItem.IsSelected ??= DetermineParentSelectionState(childItems);

                return parentItem;
            }).ToList();
    }

    /// <summary>
    /// 加载自动化列表
    /// </summary>
    /// <param name="taskData"></param>
    /// <returns></returns>
    private List<AutomaticModel> ProcessAutomaticList(List<TaskOrchestrationTable> taskData)
    {
        return taskData
            .Where(x => x.IsRelated)
            .Select(x => new AutomaticModel
            {
                Id = x.ID,
                ActionType = EnumHelper.GetDisplayName(x.OperationMode),
                Rule = x.TaskRule,
                FileFlow = $"{x.TaskSource} ➡️ {x.TaskTarget}",
                ExecutionMode = GetExecutionMode(x)
            })
            .ToList();
    }

    /// <summary>
    /// 确定父节点的选择状态
    /// </summary>
    /// <param name="childItems"></param>
    /// <returns></returns>
    private static bool? DetermineParentSelectionState(List<TaskItem> childItems)
    {
        if (childItems.All(c => c.IsSelected == true))
        {
            return true;
        }
        if (childItems.All(c => c.IsSelected == false))
        {
            return false;
        }
        return null;
    }

    /// <summary>
    /// 获取执行模式
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    private string GetExecutionMode(TaskOrchestrationTable task)
    {
        // 获取 task.Automatic 对象
        var automatic = task?.AutomaticTable;

        // 定义属性映射表
        var properties = new (Func<AutomaticTable, bool?> Selector, Func<bool> GlobalPropertyGetter, string LocalizedText)[]
        {
            (a => a?.IsFileChange, () => IsFileChange, "FileChange_Text"),
            (a => a?.RegularTaskRunning, () => RegularTaskRunning, "RegularTaskRunning_Text"),
            (a => a?.IsStartupExecution, () => IsStartupExecution, "StartupExecution_Text"),
            (a => a?.OnScheduleExecution, () => OnScheduleExecution, "ScheduleExecution_Text")
        };

        // 定义通用布尔值获取函数
        bool GetBoolValue(Func<AutomaticTable, bool?> selector, Func<bool> globalPropertyGetter)
        {
            return selector(automatic) ?? globalPropertyGetter();
        }

        // 构建返回值
        var result = new StringBuilder();
        foreach (var property in properties)
        {
            if (GetBoolValue(property.Selector, property.GlobalPropertyGetter))
            {
                if (result.Length > 0)
                {
                    result.Append(';');
                }
                result.Append(property.LocalizedText.GetLocalized());
            }
        }

        return result.ToString();
    }

    private string GetExecutionText(TaskOrchestrationTable task)
    {
        // 获取 task.Automatic 对象
        var automatic = task?.AutomaticTable;

        // 定义属性映射表
        var properties = new (Func<AutomaticTable, bool?> Selector, Func<bool> GlobalPropertyGetter, string LocalizedText)[]
        {
            (a => a?.IsFileChange, () => IsFileChange, string.Format(
                "FileChange_Text".GetLocalized() + "-" + "Latency_Text".GetLocalized(), 
                task.AutomaticTable?.DelaySeconds?.ToString() ?? "0")),
            (a => a?.RegularTaskRunning, () => RegularTaskRunning, string.Format(
                "RegularTaskRunning_Text".GetLocalized() + "-" + "RegularTask_Text".GetLocalized(), 
                task.AutomaticTable?.Hourly?.ToString() ?? "0", task.AutomaticTable?.Minutes?.ToString() ?? "0")),
            (a => a?.IsStartupExecution, () => IsStartupExecution, "StartupExecution_Text".GetLocalized()),
            (a => a?.OnScheduleExecution, () => OnScheduleExecution, "ScheduleExecution_Text".GetLocalized())
        };

        // 定义通用布尔值获取函数
        bool GetBoolValue(Func<AutomaticTable, bool?> selector, Func<bool> globalPropertyGetter)
        {
            return selector(automatic) ?? globalPropertyGetter();
        }

        // 构建返回值
        var result = new StringBuilder();
        foreach (var property in properties)
        {
            if (GetBoolValue(property.Selector, property.GlobalPropertyGetter))
            {
                if (result.Length > 0)
                {
                    result.Append(';');
                }
                result.Append(property.LocalizedText);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// 新增定时计划
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnPlanExecution()
    {
        var dialog = new PlanExecutionContentDialog
        {
            ViewModel = this,
            Title = "ScheduleText".GetLocalized(),
            PrimaryButtonText = "SaveText".GetLocalized(),
            CloseButtonText = "CancelText".GetLocalized(),
            SecondaryButtonText = "Test".GetLocalized()
        };
        dialog.PrimaryButtonClick += OnOnAddPlanPrimaryButton;

        await dialog.ShowAsync();

    }

    private async void OnOnAddPlanPrimaryButton(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            var dialog = sender as PlanExecutionContentDialog;
            if (dialog.HasErrors)
            {
                args.Cancel = true;
                return;
            }
            await _dbContext.Schedule.AddAsync(new ScheduleTable
            {
                Minutes = dialog.Minute,
                Hours = dialog.Hour,
                WeeklyDayNumber = dialog.DayOfWeek,
                DailyInMonthNumber = dialog.DayOfMonth,
                Monthly = dialog.MonthlyDay,
                CronExpression = dialog.CronExpression
            });
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Logger.Error($"添加时间表失败：{ex}");
        }
    }

    /// <summary>
    /// 保存任务配置
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnSaveTaskConfig()
    {
        IsActive = true;
        try
        {
            List<TaskOrchestrationTable> list = [];
            foreach (var item in TaskItems)
            {
                if (item.Children.Count > 0)
                {
                    var updates = await _dbContext.TaskOrchestration.Include(x => x.GroupName).Where(x => x.GroupName.Id == item.Id && x.IsRelated == true).ToListAsync();
                    if (updates != null)
                    {
                        list.AddRange(updates.Where(taskOrchestration => taskOrchestration.AutomaticTable == null)
                            .Select(taskOrchestration =>
                            {
                                var matchingModel = AutomaticModel.FirstOrDefault(model => item.Children.Any(child => child.Id == model.Id));
                                if (matchingModel != null && string.IsNullOrEmpty(matchingModel.ExecutionMode))
                                {
                                    taskOrchestration.IsRelated = false; // 设置 IsRelated 为 false
                                }
                                taskOrchestration.TaskSource = taskOrchestration.TaskSource.Equals("DesktopText".GetLocalized())
                                ? taskOrchestration.TaskSource = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                                : taskOrchestration.TaskSource;
                                return taskOrchestration;
                            }));
                    }
                }
            }
            _dbContext.TaskOrchestration.UpdateRange(list);
            DateTime dateValue = DateTime.Parse(SelectTaskTime);
            if (!RegularTaskRunning)
            {
                dateValue = dateValue = ResetToZeroTime(dateValue);
            }
            ScheduleTable schedule = null;
            if (OnScheduleExecution)
            {
                schedule = await _dbContext.Schedule.OrderByDescending(x => x.ID).FirstOrDefaultAsync();
            }
            AutomaticTable automatic = new()
            {
                IsFileChange = IsFileChange,
                DelaySeconds = DelaySeconds,
                RegularTaskRunning = RegularTaskRunning,
                Hourly = dateValue.Hour.ToString(),
                Minutes = dateValue.Minute.ToString(),
                OnScheduleExecution = OnScheduleExecution,
                IsStartupExecution = IsStartupExecution,
                Schedule = schedule,
                TaskOrchestrationList = list
            };
            await _dbContext.Automatic.AddAsync(automatic);
            await _dbContext.SaveChangesAsync();
            await OnPageLoaded();
            await AutomaticJob.AddTaskConfig(automatic, OnScheduleExecution);
            _notificationQueue.ShowWithWindowExtension("SaveSuccessfulText".GetLocalized(), InfoBarSeverity.Success);
            _ = ClearNotificationAfterDelay(3000);
        }
        catch (Exception ex)
        {
            _notificationQueue.ShowWithWindowExtension("SaveFailedText".GetLocalized(), InfoBarSeverity.Error);
            _ = ClearNotificationAfterDelay(3000);
            Logger.Error($"AutomaticViewModel: OnSaveTaskConfig 异常信息 {ex}");
            IsActive = false;
        }

        IsActive = false;

    }

    /// <summary>
    /// 更新指定任务的配置
    /// </summary>
    /// <param name="dataContent"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnCustomConfig(object dataContent)
    {
        var dialog = new CustomConfigContentDialog
        {
            ViewModel = this,
            Title = "CustomConfigurationText".GetLocalized(),
            PrimaryButtonText = "SaveText".GetLocalized(),
            CloseButtonText = "CancelText".GetLocalized(),
            SecondaryButtonText = "Test".GetLocalized()
        };

        if (dataContent is AutomaticModel data)
        {
            var old = await _dbContext.TaskOrchestration
                .Include(x => x.GroupName)
                .Include(x => x.AutomaticTable)
                .ThenInclude(a => a.Schedule)
                .Where(x => x.ID == data.Id).FirstOrDefaultAsync();
            var oldSchedule = old.AutomaticTable != null && old.AutomaticTable.Schedule != null ? old.AutomaticTable.Schedule : null;
            if (old != null)
            {
                dialog.Delay = old.AutomaticTable?.DelaySeconds ?? "5"; // 如果为 null，默认值为 0
                dialog.SelectedTime = old.AutomaticTable != null
                    ? $"{old.AutomaticTable.Hourly}:{old.AutomaticTable.Minutes}"
                    : "00:00"; // 默认时间为 "00:00"
                CustomFileChange = old.AutomaticTable?.IsFileChange ?? false; // 默认值为 false
                CustomStartupExecution = old.AutomaticTable?.IsStartupExecution ?? false; // 默认值为 false
                CustomRegularTaskRunning = old.AutomaticTable?.RegularTaskRunning ?? false; // 默认值为 false
                CustomOnScheduleExecution = old.AutomaticTable?.OnScheduleExecution ?? false; // 默认值为 false
                dialog.Minute = oldSchedule?.Minutes ?? "";
                dialog.Hour = oldSchedule?.Hours ?? "";
                dialog.DayOfWeek = oldSchedule?.WeeklyDayNumber ?? "";
                dialog.DayOfMonth = oldSchedule?.DailyInMonthNumber ?? "";
                dialog.MonthlyDay = oldSchedule?.Monthly ?? "";
                dialog.Expression = oldSchedule?.CronExpression ?? "";
            }
            if (old.AutomaticTable != null)
            {
                old.AutomaticTable.Schedule = oldSchedule;
            }
            ReceivedParameter = old;
        }

        dialog.PrimaryButtonClick += OnAddCustomConfigPrimaryButton;

        await dialog.ShowAsync();
    }

    private async void OnAddCustomConfigPrimaryButton(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            var dialog = sender as CustomConfigContentDialog;
            if (dialog.HasErrors)
            {
                args.Cancel = true;
                return;
            }

            CustomSchedule = !AreFieldsAllNullOrEmpty(dialog.Minute, dialog.Hour, dialog.DayOfMonth, dialog.MonthlyDay, dialog.DayOfWeek);

            DateTime dateValue = DateTime.Parse(dialog.SelectedTime);
            if (!CustomRegularTaskRunning)
            {
                dateValue = ResetToZeroTime(dateValue);
            }
            // 创建自动化配置对象
            var auto = CreateAutomaticTable(dialog, dateValue);

            if (ReceivedParameter is TaskOrchestrationTable old)
            {
                List<TaskOrchestrationTable> list = [];
                list.Add(old);
                // 如果 AutomaticTable 不为空且没有其他任务使用该表，则更新
                if (old.AutomaticTable != null && !await IsAutomaticTableUsedByOtherTasks(old.AutomaticTable.ID))
                {
                    UpdateAutomaticTable(old, auto, dialog);
                }
                else
                {
                    // 如果没有使用该 AutomaticTable 或者没有表，则创建一个新的
                    old.AutomaticTable = auto;
                }
                auto.TaskOrchestrationList = list;
            }

            // 保存更改到数据库并添加定时任务
            await _dbContext.SaveChangesAsync();
            await AutomaticJob.AddTaskConfig(auto, CustomSchedule);
            await OnPageLoaded();
        }
        catch (Exception ex)
        {
            Logger.Error($"添加自定义配置失败：{ex}");
        }
    }

    /// <summary>
    /// 更新选中的任务项
    /// </summary>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnUpdateChecked(object dataContext)
    {
        try
        {
            if (dataContext != null)
            {
                if (dataContext is TaskItem item)
                {
                    // 处理子节点和父节点更新逻辑
                    if (item.Children.Count > 0)
                    {
                        await UpdateChildNodes(item.Children);
                        await UpdateParentNode(item);
                    }
                    else
                    {
                        await UpdateSingleTask(item);
                        await UpdateModelByIdAsync(item);
                    }

                    // 提交所有变更
                    await _dbContext.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"AutomaticViewModel: OnUpdateChecked 异常信息 {ex}");
        }
    }

    [RelayCommand]
    private async Task OnPreviewTask(object dataContext)
    {
        var dialog = new PreviewContentDialog
        {
            ViewModel = this,
            Title = "PreviewText".GetLocalized(),
            PrimaryButtonText = "Close".GetLocalized()
        };

        if (dataContext is AutomaticModel data)
        {
            var task = await _dbContext.TaskOrchestration
                .Include(x => x.GroupName)
                .Include(x => x.Filter)
                .Include(x => x.AutomaticTable)
                .ThenInclude(s => s.Schedule)
                .Where(x => x.ID == data.Id).FirstOrDefaultAsync();
            if (task != null)
            {
                dialog.TaskName = task.TaskName;
                dialog.TaskGroup = task.GroupName.GroupName;
                dialog.FileFlow = $"{task.TaskSource} ➡️ {task.TaskTarget}";
                dialog.Rules = task.TaskRule;
                dialog.OperatingMethod = EnumHelper.GetDisplayName(task.OperationMode);
                dialog.AutomatedRules = GetExecutionText(task);
                dialog.FilterId = task.Filter?.Id.ToString() ?? "0";
                var path = task.TaskSource.Equals("DesktopText".GetLocalized())
                    ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    : task.TaskSource;
                var fileLists = FileList(path);
                FileLists = new(fileLists);
            }
        }

        await dialog.ShowAsync();
    }

    /// <summary>
    /// 更新模型
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    private async Task UpdateModelByIdAsync(TaskItem item)
    {
        // 查找对应的对象
        var modelToRemove = AutomaticModel.FirstOrDefault(model => model.Id == item.Id);

        // 如果找到对象，则从集合中移除,并删除关联
        if (modelToRemove != null && !item.IsSelected.Value)
        {
            AutomaticModel.Remove(modelToRemove);
            var update = await _dbContext.TaskOrchestration.Where(t => t.ID == item.Id).FirstOrDefaultAsync();
            if (update != null)
            {
                await RemoveAutoTask(update);
                _dbContext.Entry(update).State = EntityState.Modified;
                if (update.AutomaticTable != null)
                {
                    var delete = await _dbContext.Automatic.Where(x => x.ID == update.AutomaticTable.ID).FirstOrDefaultAsync();
                    var inUse = await _dbContext.TaskOrchestration.AnyAsync(t => t.AutomaticTable.ID == update.AutomaticTable.ID);
                    var scheduleId = update.AutomaticTable != null && update.AutomaticTable.Schedule != null ? update.AutomaticTable.Schedule.ID : 0;
                    var schedule = await _dbContext.Schedule.Where(x => x.ID == scheduleId).FirstOrDefaultAsync();
                    update.AutomaticTable = null;
                    if (delete != null && !inUse)
                    {
                        _dbContext.Automatic.Remove(delete);
                    }
                    if (schedule != null)
                    {
                        _dbContext.Schedule.Remove(schedule);
                    }
                }
            }
        }
        if (item.IsSelected.Value)
        {
            var auto = await _dbContext.TaskOrchestration
                .Include(x => x.GroupName)
                .Include(x => x.AutomaticTable)
                .Where(x => x.ID == item.Id)
                .FirstOrDefaultAsync();
            var model = new AutomaticModel
            {
                Id = auto.ID,
                ActionType = EnumHelper.GetDisplayName(auto.OperationMode),
                Rule = auto.TaskRule,
                FileFlow = $"{auto.TaskSource} ➡️ {auto.TaskTarget}",
                ExecutionMode = GetExecutionMode(auto),
            };
            AutomaticModel.Add(model);
        }

    }

    private async Task RemoveAutoTask(TaskOrchestrationTable task)
    {
        if (task.AutomaticTable != null)
        {
            if (task.AutomaticTable.IsFileChange) 
            {
                FileEventHandler.StopMonitor(task.TaskSource, task.TaskTarget);
            }
            if (task.AutomaticTable.OnScheduleExecution)
            {
                await QuartzHelper.DeleteJob($"{task.TaskName}#{task.ID}", task.GroupName.GroupName);
            }
        }
    }

    /// <summary>
    /// 更新子节点的 IsRelated 状态
    /// </summary>
    private async Task UpdateChildNodes(IEnumerable<TaskItem> children)
    {
        foreach (var child in children)
        {
            var childEntity = await _dbContext.TaskOrchestration
                .Where(x => x.ID == child.Id)
                .FirstOrDefaultAsync();

            if (childEntity != null)
            {
                childEntity.IsRelated = child.IsSelected.Value;
                _dbContext.Entry(childEntity).State = EntityState.Modified;
            }
            await UpdateModelByIdAsync(child);
        }
    }

    /// <summary>
    /// 更新单个任务项的 IsRelated 状态
    /// </summary>
    private async Task UpdateSingleTask(TaskItem item)
    {
        var update = await _dbContext.TaskOrchestration
            .Where(x => x.ID == item.Id)
            .FirstOrDefaultAsync();

        if (update != null)
        {
            update.IsRelated = item.IsSelected.Value;
            _dbContext.Entry(update).State = EntityState.Modified;
        }

        // 更新父节点的 IsUsed 状态
        await UpdateParentNode(item);
    }

    /// <summary>
    /// 更新父节点的 IsUsed 状态
    /// </summary>
    private async Task UpdateParentNode(TaskItem item)
    {
        var group = await _dbContext.TaskGroup
            .Where(x => x.Id == item.Id)
            .FirstOrDefaultAsync();

        if (group != null)
        {
            var parents = await _dbContext.TaskOrchestration
                .Where(t => t.GroupName.Id == item.Id)
                .ToListAsync();

            // 根据子节点的状态更新父节点的 IsUsed
            if (parents.All(x => x.IsRelated == true))
            {
                group.IsUsed = true;
            }
            else
            {
                group.IsUsed = false;
            }

            _dbContext.Entry(group).State = EntityState.Modified;
        }
    }

    /// <summary>
    /// 重置时间为 00:00:00
    /// </summary>
    /// <param name="dateValue"></param>
    /// <returns></returns>
    private static DateTime ResetToZeroTime(DateTime dateValue)
    {
        return new DateTime(dateValue.Year, dateValue.Month, dateValue.Day, 0, 0, 0);
    }

    /// <summary>
    /// 检查是否所有字段都为空
    /// </summary>
    /// <param name="dialogMinutes"></param>
    /// <param name="dialogHours"></param>
    /// <param name="dialogDayOfMonth"></param>
    /// <param name="dialogMonth"></param>
    /// <param name="dialogDayOfWeek"></param>
    /// <returns></returns>
    private static bool AreFieldsAllNullOrEmpty(string dialogMinutes, string dialogHours, string dialogDayOfMonth, string dialogMonth, string dialogDayOfWeek)
    {
        return string.IsNullOrEmpty(dialogMinutes)
            && string.IsNullOrEmpty(dialogHours)
            && string.IsNullOrEmpty(dialogDayOfMonth)
            && string.IsNullOrEmpty(dialogMonth)
            && string.IsNullOrEmpty(dialogDayOfWeek);
    }

    /// <summary>
    /// 创建新的 AutomaticTable 对象
    /// </summary>
    private AutomaticTable CreateAutomaticTable(CustomConfigContentDialog dialog, DateTime dateValue)
    {
        return new AutomaticTable
        {
            IsFileChange = CustomFileChange,
            IsStartupExecution = CustomStartupExecution,
            RegularTaskRunning = CustomRegularTaskRunning,
            OnScheduleExecution = CustomSchedule || !string.IsNullOrEmpty(dialog.Expression),
            DelaySeconds = dialog.Delay,
            Hourly = dateValue.Hour.ToString(),
            Minutes = dateValue.Minute.ToString(),
            Schedule = CreateScheduleTable(dialog)
        };
    }

    /// <summary>
    /// 根据对话框中的数据创建 ScheduleTable 对象
    /// </summary>
    private ScheduleTable CreateScheduleTable(CustomConfigContentDialog dialog)
    {
        return CustomSchedule || !string.IsNullOrEmpty(dialog.Expression)
            ? new ScheduleTable
            {
                Minutes = dialog.Minute,
                Hours = dialog.Hour,
                WeeklyDayNumber = dialog.DayOfWeek,
                DailyInMonthNumber = dialog.DayOfMonth,
                Monthly = dialog.MonthlyDay,
                CronExpression = dialog.Expression
            }
            : null;
    }

    /// <summary>
    /// 检查是否有其他任务正在使用该 AutomaticTable
    /// </summary>
    /// <param name="automaticTableId">AutomaticTable ID</param>
    /// <returns>如果有其他任务使用该 AutomaticTable，则返回 true</returns>
    private async Task<bool> IsAutomaticTableUsedByOtherTasks(int automaticTableId)
    {
        var task = ReceivedParameter as TaskOrchestrationTable;
        return await _dbContext.TaskOrchestration
            .AnyAsync(x => x.AutomaticTable.ID == automaticTableId && x.ID != task.ID);
    }

    /// <summary>
    /// 更新现有的 AutomaticTable 对象
    /// </summary>
    private void UpdateAutomaticTable(TaskOrchestrationTable old, AutomaticTable auto, CustomConfigContentDialog dialog)
    {
        DateTime dateValue = DateTime.Parse(SelectTaskTime);
        if (!RegularTaskRunning)
        {
            dateValue = ResetToZeroTime(dateValue);
        }
        var existingAutoTable = old.AutomaticTable;
        if (existingAutoTable != null)
        {
            existingAutoTable.IsFileChange = CustomFileChange;
            existingAutoTable.IsStartupExecution = CustomStartupExecution;
            existingAutoTable.RegularTaskRunning = CustomRegularTaskRunning;
            existingAutoTable.OnScheduleExecution = CustomSchedule || !string.IsNullOrEmpty(dialog.Expression);
            existingAutoTable.DelaySeconds = dialog.Delay;
            existingAutoTable.Hourly = dateValue.Hour.ToString();
            existingAutoTable.Minutes = dateValue.Minute.ToString();

            // 更新 Schedule
            if (existingAutoTable.Schedule != null && existingAutoTable.OnScheduleExecution)
            {
                existingAutoTable.Schedule.Minutes = dialog.Minute;
                existingAutoTable.Schedule.Hours = dialog.Hour;
                existingAutoTable.Schedule.WeeklyDayNumber = dialog.DayOfWeek;
                existingAutoTable.Schedule.DailyInMonthNumber = dialog.DayOfMonth;
                existingAutoTable.Schedule.Monthly = dialog.MonthlyDay;
                existingAutoTable.Schedule.CronExpression = dialog.Expression;
            }
            else
            {
                existingAutoTable.Schedule = CreateScheduleTable(dialog); // 如果 Schedule 为 null 则创建新 Schedule
            }
        }
    }

    /// <summary>
    /// 验证 Cron 表达式
    /// </summary>
    /// <param name="cronExpression">CRON 表达式</param>
    /// <returns></returns>
    public (bool IsValid, string Times) VerifyCronExpression(string cronExpression)
    {
        FireTimes?.Clear();
        var result = CronExpressionUtil.VerificationCronExpression(cronExpression);
        var VerificationMessage = result.Message;
        if (result.IsValid)
        {
            FireTimes = result.FireTimes.Select(time => time.ToString("yyyy-MM-dd HH:mm:ss")).ToList();
            return (true, FireTimes.Count.ToString());
        }
        else
        {
            return (false, "0");
        }

    }

    private static List<FileListModel> FileList(string path) 
    {
        var files = new List<FileListModel>();
        var dir = new DirectoryInfo(path);
        var fileInfos = dir.GetFiles();
        foreach (var fileInfo in fileInfos)
        {
            var ext = fileInfo.Extension;
            if (!ext.Equals(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(new FileListModel
                {
                    IsFolder = false,
                    Path = fileInfo.Name,
                });
            }
        }
        foreach (var directory in dir.GetDirectories())
        {
            files.Add(new FileListModel
            {
                IsFolder = true,
                Path = directory.FullName,
            });
        }
        return files;
    }

    /// <summary>
    /// 通知属性更改
    /// </summary>
    /// <param name="propertyName">属性名</param>
    /// <param name="reDoBackupDryRun"></param>
    private async void NotifyPropertyChanged([CallerMemberName] string propertyName = null, bool reDoBackupDryRun = true)
    {

        // Notify UI of property change
        OnPropertyChanged(propertyName);

        Logger.Debug($"AutomaticViewModel: NotifyPropertyChanged {propertyName}");
        GetExecutionMode(null);
        if (propertyName == nameof(IsFileChange) || propertyName == nameof(RegularTaskRunning)
            || propertyName == nameof(IsStartupExecution) || propertyName == nameof(OnScheduleExecution))
        {
            await OnPageLoaded();
        }

        UpdateCurConfig(this);

    }

    /// <summary>
    /// 清除通知
    /// </summary>
    /// <param name="delayMilliseconds"></param>
    /// <returns></returns>
    public async Task ClearNotificationAfterDelay(int delayMilliseconds)
    {
        await Task.Delay(delayMilliseconds);  // 延迟指定的毫秒数
        _notificationQueue?.Clear();  // 清除通知
    }
}
