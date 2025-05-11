// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using CommunityToolkit.WinUI;
using Quartz;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasyTidy.Views.ContentDialogs;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class CustomConfigContentDialog : ContentDialog, INotifyDataErrorInfo, INotifyPropertyChanged
{
    public AutomaticViewModel ViewModel { get; set; }

    public string SelectedTime { get; set; } = DateTime.Now.ToString("HH:mm");

    /// <summary>
    /// 延迟时间
    /// </summary>
    private string _delay = string.Empty;

    public string Delay
    {
        get => _delay;
        set
        {
            if (value != _delay)
            {
                _delay = value;
                ValidateDelay(_delay);
                OnPropertyChanged();
                UpdateIsValid();
            }
        }
    }

    /// <summary>
    /// 分钟
    /// </summary>
    private string _minute = string.Empty;

    public string Minute
    {
        get => _minute;
        set
        {
            if (value != _minute)
            {
                _minute = value;
                ValidateMinute(_minute);
                OnPropertyChanged();
                UpdateIsValid();
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
            if (value != _hour)
            {
                _hour = value;
                ValidateHour(_hour);
                OnPropertyChanged();
                UpdateIsValid();
            }
        }
    }

    /// <summary>
    /// 周
    /// </summary>
    private string _dayOfWeek = string.Empty;

    public string DayOfWeek
    {
        get => _dayOfWeek;
        set
        {
            if (value != _dayOfWeek)
            {
                _dayOfWeek = value;
                ValidateDayOfWeek(_dayOfWeek);
                OnPropertyChanged();
                UpdateIsValid();
            }
        }
    }

    /// <summary>
    /// 每月第几天
    /// </summary>
    private string _dayOfMonth = string.Empty;

    public string DayOfMonth
    {
        get => _dayOfMonth;
        set
        {
            if (value != _dayOfMonth)
            {
                _dayOfMonth = value;
                ValidateDayOfMonth(_dayOfMonth);
                OnPropertyChanged();
                UpdateIsValid();
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
            if (value != _monthlyDay)
            {
                _monthlyDay = value;
                ValidateMonthlyDay(_monthlyDay);
                OnPropertyChanged();
                UpdateIsValid();
            }
        }
    }

    /// <summary>
    /// CRON 表达式
    /// </summary>
    private string _expression;

    public string Expression
    {
        get => _expression;
        set
        {
            if (value != _expression)
            {
                _expression = value;
                ValidateCron(_expression);
                OnPropertyChanged();
            }
        }
    }

    public bool IsValid { get; set; }

    public bool DelayIsValid { get; set; }
    public bool MinuteIsValid { get; set; }
    public bool HourIsValid { get; set; }
    public bool DayOfWeekIsValid { get; set; }
    public bool DayOfMonthIsValid { get; set; }
    public bool MonthlyDayIsValid { get; set; }
    public bool ModifiedFlg { get; set; }
    private bool _isFirstLoad = true;

    public void UpdateIsValid()
    {
        IsValid = DelayIsValid && MinuteIsValid && HourIsValid &&
                  DayOfWeekIsValid && DayOfMonthIsValid && MonthlyDayIsValid;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isFirstLoad = false;
    }

    public CustomConfigContentDialog()
    {
        ViewModel = App.GetService<AutomaticViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
        RequestedTheme = ViewModel.ThemeSelectorService.Theme;
    }

    private void ValidateDelay(string delay)
    {
        var flg = ModifiedFlg;
        if (flg && _isFirstLoad)
        {
            DelayIsValid = true;
        }
        var errors = new List<string>(1);
        if (!DelayIsValid && string.IsNullOrEmpty(delay) && ViewModel.CustomFileChange)
        {
            DelayValid.Text = "ValidateDelay".GetLocalized();
            DelayValid.Visibility = Visibility.Visible;
            errors.Add("ValidateDelay".GetLocalized());
        }
        else
        {
            DelayValid.Visibility = Visibility.Collapsed;
        }
        SetErrors("Delay", errors);
    }

    /// <summary>
    /// 分钟格式验证
    /// </summary>
    /// <param name="minute"></param>
    private void ValidateMinute(string minute)
    {
        var flg = ModifiedFlg;
        if (flg && _isFirstLoad)
        {
            MinuteIsValid = true;
        }
        var errors = new List<string>(1);
        if (!MinuteIsValid && !string.IsNullOrEmpty(minute))
        {
            MinuteValid.Text = "MinuteFormatInfo".GetLocalized();
            MinuteValid.Visibility = Visibility.Visible;
            errors.Add("MinuteFormatInfo".GetLocalized());
        }
        else
        {
            MinuteValid.Visibility = Visibility.Collapsed;
        }
        SetErrors("Minute", errors);
    }

    /// <summary>
    /// 小时格式验证
    /// </summary>
    /// <param name="hour"></param>
    private void ValidateHour(string hour)
    {
        var flg = ModifiedFlg;
        if (flg && _isFirstLoad)
        {
            HourIsValid = true;
        }
        var errors = new List<string>(1);
        if (!HourIsValid && !string.IsNullOrEmpty(hour))
        {
            HourValid.Text = "HourFormatInfo".GetLocalized();
            HourValid.Visibility = Visibility.Visible;
            errors.Add("HourFormatInfo".GetLocalized());
        }
        else
        {
            HourValid.Visibility = Visibility.Collapsed;
        }
        SetErrors("Hour", errors);
    }

    /// <summary>
    /// 每周第几天验证
    /// </summary>
    /// <param name="dayOfWeek"></param>
    private void ValidateDayOfWeek(string dayOfWeek)
    {
        var flg = ModifiedFlg;
        if (flg && _isFirstLoad)
        {
            DayOfWeekIsValid = true;
        }
        var errors = new List<string>(1);
        if (!DayOfWeekIsValid && !string.IsNullOrEmpty(dayOfWeek))
        {
            DayOfWeekValid.Text = "WeeksFormatInfo".GetLocalized();
            DayOfWeekValid.Visibility = Visibility.Visible;
            errors.Add("WeeksFormatInfo".GetLocalized());
        }
        else
        {
            DayOfWeekValid.Visibility = Visibility.Collapsed;
        }
        SetErrors("DayOfWeek", errors);
    }

    /// <summary>
    /// 每月第几天验证
    /// </summary>
    /// <param name="dayOfMonth"></param>
    private void ValidateDayOfMonth(string dayOfMonth)
    {
        var flg = ModifiedFlg;
        if (flg && _isFirstLoad)
        {
            DayOfMonthIsValid = true;
        }
        var errors = new List<string>(1);
        if (!DayOfMonthIsValid && !string.IsNullOrEmpty(dayOfMonth))
        {
            DayOfMonthValid.Text = "DateFormatInfo".GetLocalized();
            DayOfMonthValid.Visibility = Visibility.Visible;
            errors.Add("DateFormatInfo".GetLocalized());
        }
        else
        {
            DayOfMonthValid.Visibility = Visibility.Collapsed;
        }
        SetErrors("DayOfMonth", errors);
    }

    /// <summary>
    /// 月份验证
    /// </summary>
    /// <param name="monthlyDay"></param>
    private void ValidateMonthlyDay(string monthlyDay)
    {
        var flg = ModifiedFlg;
        if (flg && _isFirstLoad)
        {
            MonthlyDayIsValid = true;
        }
        var errors = new List<string>(1);
        if (!MonthlyDayIsValid && !string.IsNullOrEmpty(monthlyDay))
        {
            MonthlyDayValid.Text = "MonthFormatInfo".GetLocalized();
            MonthlyDayValid.Visibility = Visibility.Visible;
            errors.Add("MonthFormatInfo".GetLocalized());
        }
        else
        {
            MonthlyDayValid.Visibility = Visibility.Collapsed;
        }
        SetErrors("MonthlyDay", errors);
    }

    /// <summary>
    /// CRON 表达式验证
    /// </summary>
    /// <param name="cron"></param>
    private void ValidateCron(string cron)
    {
        var errors = new List<string>(1);
        if (!CronExpression.IsValidExpression(cron) && !string.IsNullOrEmpty(cron))
        {
            ExpressionValid.Text = "CronExpressionInfo".GetLocalized();
            ExpressionValid.Visibility = Visibility.Visible;
            errors.Add("CronExpressionInfo".GetLocalized());
        }
        else
        {
            ExpressionValid.Visibility = Visibility.Collapsed;
        }
        SetErrors("Expression", errors);
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

    private void CustomContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        _secondaryButtonArgs = args; // 保存 args

        // 验证 Cron 表达式
        var (IsValid, Times) = !string.IsNullOrEmpty(Expression)
            ? ViewModel.VerifyCronExpression(Expression)
            : ViewModel.VerifyCronExpression(CronExpressionUtil.GenerateCronExpression(Minute, Hour, DayOfMonth, MonthlyDay, DayOfWeek));

        // 处理 TeachingTip 的显示
        CustomPlanTeachingTip.IsOpen = true;
        CustomPlanTeachingTip.Subtitle = IsValid
            ? string.Format("VerificationSuccessful".GetLocalized(), Times)
            : "VerificationFailed".GetLocalized();

        // 取消 ContentDialog 的关闭
        args.Cancel = true;
        CustomPlanTeachingTip.CloseButtonContent = "Close".GetLocalized();
        CustomPlanTeachingTip.CloseButtonClick += CustomPlanTeachingTip_CloseButtonClick;
    }

    private void CustomPlanTeachingTip_CloseButtonClick(TeachingTip sender, object args)
    {
        // 关闭 TeachingTip
        sender.IsOpen = false;

        // 允许关闭 ContentDialog
        if (_secondaryButtonArgs != null)
        {
            _secondaryButtonArgs.Cancel = false;
        }
    }

    private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        var resutl = sender as ToggleSwitch;
        if (!resutl.IsOn)
        {
            DelayValid.Visibility = Visibility.Collapsed;
        }
    }
}
