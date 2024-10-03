using CommunityToolkit.WinUI.UI;
using EasyTidy.Model;
using EasyTidy.Util;
using EasyTidy.Views.ContentDialogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;
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

        await dialog.ShowAsync();

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

        Logger.Info($"GeneralViewModel: NotifyPropertyChanged {propertyName}");

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
                    var list = await db.FileExplorer.ToListAsync();
                    foreach (var item in list)
                    {
                        if (item.TaskSource == Environment.GetFolderPath(Environment.SpecialFolder.Desktop))
                        {
                            item.TaskSource = "桌面";
                        }
                    }
                    TaskList = new(await db.FileExplorer.ToListAsync());
                    TaskListACV = new AdvancedCollectionView(TaskList, true);
                    TaskListACV.SortDescriptions.Add(new SortDescription("ID", SortDirection.Ascending));
                });
            });

        }
        catch (Exception ex)
        {
            IsActive = false;
            Logger.Error($"ServerViewModel: OnPageLoad 异常信息 {ex}");
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
                var item = dataContext as ListView;
                var items = item.SelectedItems;
                var s = item.SelectedItem as FileExplorerTable;
                await using var db = new AppDbContext();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"ServerViewModel: OnSelectedItemChanged 异常信息 {ex}");
        }

    }

}
