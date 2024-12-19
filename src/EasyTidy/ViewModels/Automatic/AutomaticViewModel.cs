using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using CommunityToolkit.WinUI.Collections;
using EasyTidy.Common.Database;
using EasyTidy.Common.Extensions;
using EasyTidy.Common.Job;
using EasyTidy.Common.Model;
using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using EasyTidy.Views.ContentDialogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

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

    /// <summary>
    /// 是否单独配置标识
    /// </summary>
    private bool _featureIndependentConfigFlag = false;

    public bool FeatureIndependentConfigFlag
    {
        get => _featureIndependentConfigFlag;
        set
        {
            if (_featureIndependentConfigFlag != value)
            {
                _featureIndependentConfigFlag = value;
                NotFeatureIndependentConfigFlag = !value;
                OnPropertyChanged();
            }
        }

    }

    [ObservableProperty]
    public bool _notFeatureIndependentConfigFlag = true;

    [ObservableProperty]
    public bool _groupGlobalIsOpen = false;

    [ObservableProperty]
    public bool _customIsOpen = false;

    [ObservableProperty]
    public bool _customFileChange = false;

    [ObservableProperty]
    public bool _customStartupExecution = false;

    [ObservableProperty]
    public bool _customRegularTaskRunning = false;

    [ObservableProperty]
    public bool _customOnScheduleExecution = false;

    [ObservableProperty]
    public ObservableCollection<TaskOrchestrationTable> _taskList;

    [ObservableProperty]
    public AdvancedCollectionView _taskListACV;

    [ObservableProperty]
    public ObservableCollection<TaskGroupTable> _taskGroupList;

    [ObservableProperty]
    public AdvancedCollectionView _taskGroupListACV;

    [ObservableProperty]
    private ObservableCollection<TaskItem> _taskItems;

    [ObservableProperty]
    private ObservableCollection<AutomaticModel> _automaticModel;

    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    [ObservableProperty]
    private ObservableCollection<TaskOrchestrationTable> _selectedTaskList = [];

    [ObservableProperty]
    private ObservableCollection<TaskGroupTable> _selectedGroupTaskList = [];

    [ObservableProperty]
    private string _delaySeconds = "5";

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


    [RelayCommand]
    private async Task OnCustomConfig()
    {
        var dialog = new CustomConfigContentDialog
        {
            ViewModel = this,
            Title = "CustomConfigurationText".GetLocalized(),
            PrimaryButtonText = "SaveText".GetLocalized(),
            CloseButtonText = "CancelText".GetLocalized(),
            SecondaryButtonText = "Test".GetLocalized()
        };

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

            List<TaskOrchestrationTable> list = [];

            foreach (var item in SelectedTaskList)
            {
                var update = await _dbContext.TaskOrchestration.Where(x => x.ID == item.ID && item.IsRelated == false).FirstOrDefaultAsync();
                if (update != null)
                {
                    update.IsRelated = true;
                    update.TaskSource = update.TaskSource.Equals("DesktopText".GetLocalized())
                        ? update.TaskSource = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                        : update.TaskSource;
                    _dbContext.Entry(update).State = EntityState.Modified;
                    list.Add(update);
                }
            }

            DateTime dateValue = DateTime.Parse(dialog.SelectedTime);
            if (!CustomRegularTaskRunning)
            {
                dateValue = new DateTime(dateValue.Year, dateValue.Month, dateValue.Day, 0, 0, 0);
            }
            var auto = new AutomaticTable
            {
                IsFileChange = CustomFileChange,
                IsStartupExecution = CustomStartupExecution,
                RegularTaskRunning = CustomRegularTaskRunning,
                OnScheduleExecution = CustomOnScheduleExecution,
                DelaySeconds = dialog.Delay,
                Hourly = dateValue.Hour.ToString(),
                Minutes = dateValue.Minute.ToString(),
                Schedule = CustomSchedule ? new ScheduleTable
                {
                    Minutes = dialog.Minute,
                    Hours = dialog.Hour,
                    WeeklyDayNumber = dialog.DayOfWeek,
                    DailyInMonthNumber = dialog.DayOfMonth,
                    Monthly = dialog.MonthlyDay,
                    CronExpression = dialog.Expression
                } : null,
                TaskOrchestrationList = list
            };
            await _dbContext.Automatic.AddAsync(auto);

            await _dbContext.SaveChangesAsync();
            await AutomaticJob.AddTaskConfig(auto, CustomSchedule);
        }
        catch (Exception ex)
        {
            Logger.Error($"添加自定义配置失败：{ex}");
        }
    }

    [RelayCommand]
    private void OnSelectTask()
    {
        GroupGlobalIsOpen = true;
        CustomIsOpen = false;
    }

    [RelayCommand]
    private void OnCustomSelectTask()
    {
        CustomIsOpen = true;
        GroupGlobalIsOpen = false;
    }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = null, bool reDoBackupDryRun = true)
    {

        // Notify UI of property change
        OnPropertyChanged(propertyName);

        Logger.Info($"AutomaticViewModel: NotifyPropertyChanged {propertyName}");

        UpdateCurConfig(this);

    }

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
                    var auto = await _dbContext.Automatic.Include(x => x.TaskOrchestrationList).Include(x => x.Schedule).FirstOrDefaultAsync();
                    TaskList = new(list);
                    TaskListACV = new AdvancedCollectionView(TaskList, true);
                    TaskListACV.SortDescriptions.Add(new SortDescription("ID", SortDirection.Ascending));
                    var groupList = await _dbContext.TaskGroup.Include(g => g.TaskOrchestrationList).ToListAsync();
                    TaskGroupList = new(groupList);
                    TaskGroupListACV = new AdvancedCollectionView(TaskGroupList, true);
                    TaskGroupListACV.SortDescriptions.Add(new SortDescription("Id", SortDirection.Ascending));
                    var isExpanded = true;
                    if (list.Count > 5)
                    {
                        isExpanded = false;
                    }
                    var groupTask = list.GroupBy(x => x.GroupName.GroupName).Select(g =>
                    {
                        // 获取根节点的 isUsed 字段
                        var isUsed = g.FirstOrDefault()?.GroupName.IsUsed ?? false;

                        // 创建父节点，并设置其默认值
                        var parentItem = new TaskItem(null)
                        {
                            Name = g.Key,
                            IsExpanded = isExpanded
                        };

                        // 创建子项
                        var childItems = g.Select(x => new TaskItem(parentItem)
                        {
                            Name = x.TaskName,
                            IsSelected = x.IsRelated // 子项的 IsSelected 根据 IsRelated 设置
                        }).ToList();

                        // 将子项加入父节点的 Children 集合
                        foreach (var childItem in childItems)
                        {
                            parentItem.Children.Add(childItem);
                        }

                        // 更新父节点的 IsSelected
                        parentItem.UpdateIsSelected();

                        // 根节点的 IsSelected 仅在其为 null 时使用子项的状态或 isUsed 进行更新
                        if (parentItem.IsSelected == null)
                        {
                            // 如果所有子项都被选中，则父项为 true
                            parentItem.IsSelected = childItems.All(c => c.IsSelected == true) ? true :
                                                   // 如果所有子项都未选中，则父项为 false
                                                   childItems.All(c => c.IsSelected == false) ? false :
                                                   // 如果部分选中，则父项为 null
                                                   (bool?)null;
                        }
                        return parentItem;
                    }).ToList();

                    TaskItems = new ObservableCollection<TaskItem>(groupTask);

                    var autoList = list.Where(x => x.IsRelated == true).Select(x => new AutomaticModel
                    {
                        ActionType = EnumHelper.GetDisplayName(x.OperationMode),
                        Rule = x.TaskRule,
                        FileFlow = $"{x.TaskSource} -> {x.TaskTarget}",
                        ExecutionMode = GetExecutionMode(list.FirstOrDefault()),
                    }).ToList();

                    AutomaticModel = new(autoList);



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

    private string GetExecutionMode(TaskOrchestrationTable task)
    {
        if (task != null)
        {
            if (task.Automatic.IsFileChange && task.Automatic.RegularTaskRunning) 
            {
                return "FileChangeText, RegularTaskRunningText";
            }
            if (task.Automatic.IsStartupExecution)
            {
                return "StartupExecutionText";
            }
            if (task.Automatic.OnScheduleExecution)
            {
                return "ScheduleExecutionText";
            }
            if (task.Automatic.IsFileChange)
            {
                return "FileChangeText";
            }
            if (task.Automatic.RegularTaskRunning)
            {
                return "RegularTaskRunningText";
            }
        }
        return string.Empty;
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
                var items = listView.SelectedItems;
                SelectedTaskList.Clear();
                foreach (var item in items)
                {
                    TaskOrchestrationTable task = item as TaskOrchestrationTable;
                    SelectedTaskList.Add(task);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"AutomaticViewModel: OnSelectedItemChanged 异常信息 {ex}");
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OnSelectGroupItemChanged(object dataContext)
    {
        try
        {
            if (dataContext != null)
            {
                var listViews = dataContext as TeachingTip;
                var listView = listViews.HeroContent as ListView;
                var items = listView.SelectedItems;
                SelectedGroupTaskList.Clear();
                foreach (var item in items)
                {
                    TaskGroupTable task = item as TaskGroupTable;
                    SelectedGroupTaskList.Add(task);
                }
                await OnPageLoaded();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"AutomaticViewModel: OnSelectGroupItemChanged 异常信息 {ex}");
        }
    }

    [RelayCommand]
    private async Task OnSaveTaskConfig()
    {
        IsActive = true;
        try
        {
            List<TaskOrchestrationTable> list = [];
            foreach (var item in SelectedGroupTaskList)
            {
                var updates = await _dbContext.TaskOrchestration.Include(x => x.GroupName).Where(x => x.GroupName.Id == item.Id).ToListAsync();
                if (updates != null)
                {
                    list.AddRange(updates.Select(taskOrchestration =>
                    {
                        taskOrchestration.IsRelated = true;
                        taskOrchestration.GroupName.IsUsed = true;
                        taskOrchestration.TaskSource = taskOrchestration.TaskSource.Equals("DesktopText".GetLocalized())
                        ? taskOrchestration.TaskSource = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                        : taskOrchestration.TaskSource;
                        return taskOrchestration;
                    }));
                }
            }
            _dbContext.TaskOrchestration.UpdateRange(list);
            await _dbContext.SaveChangesAsync();
            await OnPageLoaded();
            DateTime dateValue = DateTime.Parse(SelectTaskTime);
            if (!RegularTaskRunning)
            {
                dateValue = new DateTime(dateValue.Year, dateValue.Month, dateValue.Day, 0, 0, 0);
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

    public async Task ClearNotificationAfterDelay(int delayMilliseconds)
    {
        await Task.Delay(delayMilliseconds);  // 延迟指定的毫秒数
        _notificationQueue?.Clear();  // 清除通知
    }
}
