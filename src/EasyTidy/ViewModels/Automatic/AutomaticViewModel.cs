﻿using CommunityToolkit.WinUI.UI;
using EasyTidy.Model;
using EasyTidy.Util;
using EasyTidy.Views.ContentDialogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Dispatching;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasyTidy.ViewModels;

public partial class AutomaticViewModel : ObservableRecipient
{
    public AutomaticViewModel(IThemeService themeService)
    {
        this.themeService = themeService;
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
    [ObservableProperty]
    public bool _featureIndependentConfigFlag = false;

    [ObservableProperty]
    public bool _globalIsOpen = false;

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
    public ObservableCollection<FileExplorerTable> _taskList;

    [ObservableProperty]
    public AdvancedCollectionView _taskListACV;

    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    [ObservableProperty]
    private ObservableCollection<FileExplorerTable> _selectedTaskList = [];

    [ObservableProperty]
    private string _delaySeconds = "5";

    [ObservableProperty]
    private string _selectTaskTime = DateTime.Now.ToString("HH:mm");


    [RelayCommand]
    private async Task OnPlanExecution()
    {
        var dialog = new PlanExecutionContentDialog
        {
            ViewModel = this,
            Title = "时间表",
            PrimaryButtonText = "保存",
            CloseButtonText = "取消",
            ThemeService = themeService
        };
        dialog.PrimaryButtonClick += OnOnAddPlanPrimaryButton;

        await dialog.ShowAsync();

    }

    private async void OnOnAddPlanPrimaryButton(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            await using var db = new AppDbContext();
            var dialog = sender as PlanExecutionContentDialog;
            if (dialog.HasErrors)
            {
                return;
            }
            await db.Schedule.AddAsync(new ScheduleTable
            {
                Minutes = dialog.Minute,
                Hours = dialog.Hour,
                WeeklyDayNumber = dialog.DayOfWeek,
                DailyInMonthNumber = dialog.DayOfMonth,
                Monthly = dialog.MonthlyDay,
                CronExpression = dialog.CronExpression
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Logger.Error($"添加自定义配置失败：{ex}");
        }
    }


    [RelayCommand]
    private async Task OnCustomConfig()
    {
        var dialog = new CustomConfigContentDialog
        {
            ViewModel = this,
            Title = "自定义配置",
            PrimaryButtonText = "保存",
            CloseButtonText = "取消",
        };

        await dialog.ShowAsync();
    }

    
    [RelayCommand]
    private void OnSelectTask(object parameter)
    {
        var item = parameter as Button;
        var name = item.Name;
        if (string.Equals(name, "SelectButton", StringComparison.OrdinalIgnoreCase))
        {
            GlobalIsOpen = true;
            CustomIsOpen = false;
        }

        if (string.Equals(name, "CustomTaskList", StringComparison.OrdinalIgnoreCase))
        {
            GlobalIsOpen = false;
            CustomIsOpen = true;
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
                    await using var db = new AppDbContext();
                    var list = await db.FileExplorer.Where(f => f.IsRelated == false).ToListAsync();
                    TaskList = new(list);
                    TaskListACV = new AdvancedCollectionView(TaskList, true);
                    TaskListACV.SortDescriptions.Add(new SortDescription("ID", SortDirection.Ascending));
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
    private async Task OnSelectedItemChanged(object dataContext)
    {
        try
        {
            if (dataContext != null)
            {
                var listViews = dataContext as TeachingTip;
                var listView = listViews.HeroContent as ListView;
                var items = listView.SelectedItems;
                foreach (var item in items)
                {
                    FileExplorerTable task = item as FileExplorerTable;
                    SelectedTaskList.Add(task);
                }
                await OnPageLoaded();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"AutomaticViewModel: OnSelectedItemChanged 异常信息 {ex}");
        }

    }

    [RelayCommand]
    private async Task OnSaveTaskConfig()
    {
        IsActive = true;
        try
        {
            await using var db = new AppDbContext();
            List<FileExplorerTable> list = [];
            foreach (var item in SelectedTaskList)
            {
                var update = await db.FileExplorer.Where(x => x.ID == item.ID && item.IsRelated == false).FirstOrDefaultAsync();
                if (update != null)
                {
                    update.IsRelated = true;
                    db.Entry(update).State = EntityState.Modified;
                    list.Add(update);
                }
            }
            await db.SaveChangesAsync();
            await OnPageLoaded();
            DateTime dateValue = DateTime.Parse(SelectTaskTime);
            if (!RegularTaskRunning)
            {
                dateValue = new DateTime(dateValue.Year, dateValue.Month, dateValue.Day, 0, 0, 0);
            }
            ScheduleTable schedule = null;
            if (OnScheduleExecution)
            {
                schedule = await db.Schedule.OrderByDescending(x => x.ID).FirstOrDefaultAsync();
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
                FileExplorerList = list
            };
            await db.Automatic.AddAsync(automatic);
            await db.SaveChangesAsync();
            await OnPageLoaded();
            Growl.Success(new GrowlInfo
            {
                Message = "保存成功",
                ShowDateTime = false
            });
        }
        catch (Exception ex)
        {
            Growl.Error(new GrowlInfo
            {
                Message = "保存失败",
                ShowDateTime = false
            });
            Logger.Error($"AutomaticViewModel: OnSaveTaskConfig 异常信息 {ex}");
            IsActive = false;
        }

        IsActive = false;

    }

}