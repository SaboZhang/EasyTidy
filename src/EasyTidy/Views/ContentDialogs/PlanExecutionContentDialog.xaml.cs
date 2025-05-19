// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using CommunityToolkit.WinUI;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace EasyTidy.Views.ContentDialogs;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class PlanExecutionContentDialog : ContentDialog, INotifyDataErrorInfo, INotifyPropertyChanged
{

    public AutomaticViewModel ViewModel { get; set; }

    /// <summary>
    /// 分钟
    /// </summary>
    private string _minute = string.Empty;

    public string Minute
    {
        get => _minute;
        set
        {
            if (_minute != value)
            {
                _minute = value;
                ValidateMinute(_minute);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 小时
    /// </summary>
    private string _hour = string.Empty;
    public string Hour
    {
        get => _hour;
        set
        {
            if (_hour != value)
            {
                _hour = value;
                ValidateHour(_hour);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 每个星期的第几天
    /// </summary>
    private string _dayOfWeek = string.Empty;
    public string DayOfWeek
    {
        get => _dayOfWeek;
        set
        {
            if (_dayOfWeek != value)
            {
                _dayOfWeek = value;
                ValidateDayOfWeek(_dayOfWeek);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 每个月的第几天
    /// </summary>
    private string _dayOfMonth = string.Empty;

    public string DayOfMonth
    {
        get => _dayOfMonth;
        set
        {
            if (_dayOfMonth != value)
            {
                _dayOfMonth = value;
                ValidateDayOfMonth(_dayOfMonth);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 月份
    /// </summary>
    private string _monthlyDay = string.Empty;

    public string MonthlyDay
    {
        get => _monthlyDay;
        set
        {
            if (_monthlyDay != value)
            {
                _monthlyDay = value;
                ValidateMonthlyDay(_monthlyDay);
                OnPropertyChanged();
            }

        }
    }

    /// <summary>
    /// Cron 表达式
    /// </summary>
    private string _cronExpression = string.Empty;

    public string CronExpression
    {
        get => _cronExpression;
        set
        {
            if (_cronExpression != value)
            {
                _cronExpression = value;
                ValidateCron(_cronExpression);
                OnPropertyChanged();
            }

        }
    }

    public string QuickTime { get; set; } = DateTime.Now.ToString("HH:mm");

    public bool IsValid { get; set; }

    public PlanExecutionContentDialog()
    {
        ViewModel = App.GetService<AutomaticViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
        RequestedTheme = ViewModel.ThemeSelectorService.Theme;
        ValidateCron(_cronExpression);
        ValidateMinute(_minute);
        ValidateHour(_hour);
        ValidateDayOfWeek(_dayOfWeek);
        ValidateDayOfMonth(_dayOfMonth);
        ValidateMonthlyDay(_monthlyDay);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Minute = string.Empty;
        Hour = string.Empty;
    }

    /// <summary>
    /// 分钟验证
    /// </summary>
    /// <param name="minute"></param>
    private void ValidateMinute(string minute)
    {
        var errors = new List<string>(1);
        var pattern = new Regex(@"^(0?[0-9]|[1-5][0-9])(,(0?[0-9]|[1-5][0-9]))*$");
        if (!pattern.IsMatch(minute) && !string.IsNullOrWhiteSpace(minute))
        {
            errors.Add("MinuteFormatInfo".GetLocalized());
        }
        SetErrors("Minute", errors);
    }

    /// <summary>
    /// 小时验证
    /// </summary>
    /// <param name="hour"></param>
    private void ValidateHour(string hour)
    {
        var errors = new List<string>(1);
        var pattern = new Regex(@"^(2[0-3]|[01]?[0-9])(,(2[0-3]|[01]?[0-9]))*$");
        if (!pattern.IsMatch(hour) && !string.IsNullOrWhiteSpace(hour))
        {
            errors.Add("HourFormatInfo".GetLocalized());
        }
        SetErrors("Hour", errors);
    }

    /// <summary>
    /// 每周第几天
    /// </summary>
    /// <param name="dayOfWeek"></param>
    private void ValidateDayOfWeek(string dayOfWeek)
    {
        var errors = new List<string>(1);
        var pattern = new Regex(@"^(0|1|2|3|4|5|6)(,(0|1|2|3|4|5|6))*$");
        if (!pattern.IsMatch(dayOfWeek) && !string.IsNullOrWhiteSpace(dayOfWeek))
        {
            errors.Add("WeeksFormatInfo".GetLocalized());
        }
        SetErrors("DayOfWeek", errors);
    }

    /// <summary>
    /// 每月的第几天的验证
    /// </summary>
    /// <param name="dayOfMonth"></param>
    private void ValidateDayOfMonth(string dayOfMonth)
    {
        var errors = new List<string>(1);
        var pattern = new Regex(@"^(31|30|[12][0-9]|1?[1-9])(,(31|30|[12][0-9]|1?[1-9]))*$");
        if (!pattern.IsMatch(dayOfMonth) && !string.IsNullOrWhiteSpace(dayOfMonth))
        {
            errors.Add("DateFormatInfo".GetLocalized());
        }
        SetErrors("DayOfMonth", errors);
    }

    /// <summary>
    /// 月份验证
    /// </summary>
    /// <param name="monthlyDay"></param>
    private void ValidateMonthlyDay(string monthlyDay)
    {
        var errors = new List<string>(1);
        var pattern = new Regex(@"^(1|2|3|4|5|6|7|8|9|10|11|12)(,(1|2|3|4|5|6|7|8|9|10|11|12))*$");
        if (!pattern.IsMatch(monthlyDay) && !string.IsNullOrWhiteSpace(monthlyDay))
        {
            errors.Add("MonthFormatInfo".GetLocalized());
        }
        SetErrors("MonthlyDay", errors);
    }

    /// <summary>
    /// 验证CRON表达式
    /// </summary>
    /// <param name="cron"></param>
    private void ValidateCron(string cron)
    {
        var errors = new List<string>(1);
        if (!Quartz.CronExpression.IsValidExpression(cron) && !string.IsNullOrWhiteSpace(cron))
        {
            errors.Add("CronExpressionInfo".GetLocalized());
        }
        SetErrors("CronExpression", errors);
    }

    public bool HasErrors => _validationErrors.Count > 0;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new(propertyName));

    // Error validation
    private readonly Dictionary<string, ICollection<string>> _validationErrors = [];

    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
    public event PropertyChangedEventHandler PropertyChanged;

    private void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new(propertyName));
        OnPropertyChanged(nameof(HasErrors));
    }

    public IEnumerable GetErrors(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) ||
            !_validationErrors.ContainsKey(propertyName))
            return null;

        return _validationErrors[propertyName];
    }

    private void SetErrors(string key, ICollection<string> errors)
    {
        if (errors.Any())
            _validationErrors[key] = errors;
        else
            _ = _validationErrors.Remove(key);

        OnErrorsChanged(key);
    }

    private ContentDialogButtonClickEventArgs _secondaryButtonArgs;

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        _secondaryButtonArgs = args; // 保存 args

        // 验证 Cron 表达式
        var (IsValid, Times) = !string.IsNullOrEmpty(CronExpression)
            ? ViewModel.VerifyCronExpression(CronExpression)
            : ViewModel.VerifyCronExpression(CronExpressionUtil.GenerateCronExpression(Minute, Hour, DayOfMonth, MonthlyDay, DayOfWeek));

        // 处理 TeachingTip 的显示
        PlanTeachingTip.IsOpen = true;
        PlanTeachingTip.Subtitle = IsValid
            ? string.Format("VerificationSuccessful".GetLocalized(), Times)
            : "VerificationFailed".GetLocalized();

        // 取消 ContentDialog 的关闭
        args.Cancel = true;
        PlanTeachingTip.CloseButtonContent = "Close".GetLocalized();
        PlanTeachingTip.CloseButtonClick += PlanTeachingTip_CloseButtonClick;
    }

    private void PlanTeachingTip_CloseButtonClick(TeachingTip sender, object args)
    {
        // 关闭 TeachingTip
        sender.IsOpen = false;

        // 允许关闭 ContentDialog
        if (_secondaryButtonArgs != null)
        {
            _secondaryButtonArgs.Cancel = false;
        }
    }

    private void TimePicker_SelectedTimeChanged(TimePicker sender, TimePickerSelectedValueChangedEventArgs args)
    {
        if (args.NewTime != null)
        {
            var time = args.NewTime.Value;
            Hour = time.Hours.ToString();
            Minute = time.Minutes.ToString();
        }
    }
}
