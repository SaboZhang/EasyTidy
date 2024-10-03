using EasyTidy.Model;
using EasyTidy.Views.ContentDialogs;
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
    private bool _isFileChange;

    public bool IsFileChange
    {
        get => _isFileChange;
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
    private bool _isStartupExecution;

    public bool IsStartupExecution
    {
        get => _isStartupExecution;
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
    private bool _regularTaskRunning;

    public bool RegularTaskRunning
    {
        get => _regularTaskRunning;
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
    private bool _onScheduleExecution;

    public bool OnScheduleExecution
    {

        get => _onScheduleExecution;
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

}
