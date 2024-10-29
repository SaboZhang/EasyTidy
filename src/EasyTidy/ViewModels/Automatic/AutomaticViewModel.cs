using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using EasyTidy.Common.Database;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.ViewModels.Automatic;
using EasyTidy.Views.ContentDialogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Dispatching;
using Quartz;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace EasyTidy.ViewModels;

public partial class AutomaticViewModel : ObservableRecipient, IJob
{
    private readonly AppDbContext _dbContext;
    public AutomaticViewModel(IThemeService themeService)
    {
        this.themeService = themeService;
        _dbContext = App.GetService<AppDbContext>();
    }

    public AutomaticViewModel()
    {

    }

    public IThemeService themeService;

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
    public bool _globalIsOpen = false;

    [ObservableProperty]
    public bool _groupGlobalIsOpen = false;

    [ObservableProperty]
    public bool _customGroupIsOpen = false;

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

    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    [ObservableProperty]
    private ObservableCollection<TaskOrchestrationTable> _selectedTaskList = [];

    [ObservableProperty]
    private ObservableCollection<TaskGroupTable> _selectedGroupTaskList = [];

    [ObservableProperty]
    private string _delaySeconds = "5";

    [ObservableProperty]
    private string _selectTaskTime = DateTime.Now.ToString("HH:mm");

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


    [RelayCommand]
    private async Task OnPlanExecution()
    {
        var dialog = new PlanExecutionContentDialog
        {
            ViewModel = this,
            Title = "ScheduleText".GetLocalized(),
            PrimaryButtonText = "SaveText".GetLocalized(),
            CloseButtonText = "CancelText".GetLocalized(),
            ThemeService = themeService
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
                    _dbContext.Entry(update).State = EntityState.Modified;
                    list.Add(update);
                }
            }

            foreach (var item in SelectedGroupTaskList)
            {
                var updates = await _dbContext.TaskOrchestration.Include(x => x.GroupName).Where(x => x.GroupName.Id == item.Id).ToListAsync();
                if (updates != null)
                {
                    list.AddRange(updates.Select(taskOrchestration =>
                    {
                        taskOrchestration.IsRelated = true;
                        taskOrchestration.GroupName.IsUsed = true;
                        return taskOrchestration;
                    }));
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
            var customViewModel = new AutomaticCustomViewModel();
            await customViewModel.AddCustomTaskConfig(auto, CustomSchedule);
        }
        catch (Exception ex)
        {
            Logger.Error($"添加自定义配置失败：{ex}");
        }
    }

    [RelayCommand]
    private void OnSelectTask(object parameter)
    {
        var item = parameter as Button;
        var name = item.Name;

        switch (name)
        {
            case "SelectButton":
                GlobalIsOpen = true;
                GroupGlobalIsOpen = false;
                break;
            case "GroupButton":
                GlobalIsOpen = false;
                GroupGlobalIsOpen = true;
                break;
        }
    }

    [RelayCommand]
    private void OnCustomSelectTask(object parameter)
    {
        var item = parameter as Button;
        var name = item.Name;

        switch (name)
        {
            case "CustomTaskList":
                CustomIsOpen = true;
                CustomGroupIsOpen = false;
                break;
            case "CustomGroupButton":
                CustomIsOpen = false;
                CustomGroupIsOpen = true;
                break;
        }
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
                    var list = await _dbContext.TaskOrchestration.Include(x => x.GroupName).Where(f => f.IsRelated == false).ToListAsync();
                    TaskList = new(list);
                    TaskListACV = new AdvancedCollectionView(TaskList, true);
                    TaskListACV.SortDescriptions.Add(new SortDescription("ID", SortDirection.Ascending));
                    var groups = await _dbContext.TaskGroup.Include(g => g.TaskOrchestrationList).Where(x => x.IsUsed == false).ToListAsync();
                    // 过滤分组：当某个分组的所有关联任务 IsRelated 为 true 时不显示该分组
                    var groupList = groups
                        .Where(g => g.Tasks.All(t => t.IsRelated == false)) // 只显示与 IsRelated 为 false 的分组
                        .ToList();
                    TaskGroupList = new(groupList);
                    TaskGroupListACV = new AdvancedCollectionView(TaskGroupList, true);
                    TaskGroupListACV.SortDescriptions.Add(new SortDescription("Id", SortDirection.Ascending));
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
            foreach (var item in SelectedTaskList)
            {
                var update = await _dbContext.TaskOrchestration.Where(x => x.ID == item.ID && item.IsRelated == false).FirstOrDefaultAsync();
                if (update != null)
                {
                    update.IsRelated = true;
                    list.Add(update);
                }
            }
            foreach (var item in SelectedGroupTaskList)
            {
                var updates = await _dbContext.TaskOrchestration.Include(x => x.GroupName).Where(x => x.GroupName.Id == item.Id).ToListAsync();
                if (updates != null)
                {
                    list.AddRange(updates.Select(taskOrchestration =>
                    {
                        taskOrchestration.IsRelated = true;
                        taskOrchestration.GroupName.IsUsed = true;
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
                if (!string.IsNullOrEmpty(schedule.CronExpression))
                {
                    foreach (var item in list)
                    {
                        await QuartzHelper.AddJob<AutomaticViewModel>(item.TaskName, item.GroupName.GroupName, schedule.CronExpression);
                    }
                }
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
            Growl.Success(new GrowlInfo
            {
                Message = "SaveSuccessfulText".GetLocalized(),
                ShowDateTime = false
            });
        }
        catch (Exception ex)
        {
            Growl.Error(new GrowlInfo
            {
                Message = "SaveFailedText".GetLocalized(),
                ShowDateTime = false
            });
            Logger.Error($"AutomaticViewModel: OnSaveTaskConfig 异常信息 {ex}");
            IsActive = false;
        }

        IsActive = false;

    }

    public async Task Execute(IJobExecutionContext context)
    {
        Logger.Info(context.JobDetail.ToString());
        // 获取数据库枚举值
        var mode = 0;
        await OperationHandler.ExecuteOperationAsync(mode, "示例参数1");
    }
}
