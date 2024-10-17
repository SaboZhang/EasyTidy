// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Quartz;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace EasyTidy.Views.ContentDialogs;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class CustomConfigContentDialog : ContentDialog, INotifyDataErrorInfo, INotifyPropertyChanged
{
    public AutomaticViewModel ViewModel { get; set; }

    public string SelectedTime { get; set; } = DateTime.Now.ToString("HH:mm");

    /// <summary>
    /// �ӳ�
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
            }
        }
    }

    /// <summary>
    /// ����
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
            }
        }
    }

    /// <summary>
    /// Сʱ
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
            }
        }
    }

    /// <summary>
    /// ����
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
            }
        }
    }

    /// <summary>
    /// ÿ�µĵڼ���
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
            }
        }
    }

    /// <summary>
    /// �·�
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
            }
        }
    }

    /// <summary>
    /// ���ʽ
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


    public CustomConfigContentDialog()
    {
        ViewModel = App.GetService<AutomaticViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
        RequestedTheme = ViewModel.themeService.GetElementTheme();
        ValidateDelay(_delay);
        ValidateMinute(_minute);
        ValidateHour(_hour);
        ValidateDayOfWeek(_dayOfWeek);
        ValidateDayOfMonth(_dayOfMonth);
        ValidateMonthlyDay(_monthlyDay);
    }

    private void ValidateDelay(string delay)
    {
        var errors = new List<string>(1);
        var pattern = new Regex(@"^\d+$");
        if (!pattern.IsMatch(delay) && !string.IsNullOrWhiteSpace(delay))
        {
            errors.Add("�ӳ�ֻ����������");
        }
        SetErrors("Delay", errors);
    }

    /// <summary>
    /// ������֤
    /// </summary>
    /// <param name="minute"></param>
    private void ValidateMinute(string minute)
    {
        var errors = new List<string>(1);
        var pattern = new Regex(@"^([1-9]|[1-5][0-9])(,(?=[1-9]|[1-5][0-9]))*$");
        if (!pattern.IsMatch(minute) && !string.IsNullOrWhiteSpace(minute))
        {
            errors.Add("���Ӹ�ʽ����");
        }
        SetErrors("Minute", errors);
    }

    /// <summary>
    /// Сʱ��֤
    /// </summary>
    /// <param name="hour"></param>
    private void ValidateHour(string hour)
    {
        var errors = new List<string>(1);
        var pattern = new Regex(@"^(2[0-3]|[01]?[0-9])(,(2[0-3]|[01]?[0-9]))*$");
        if (!pattern.IsMatch(hour) && !string.IsNullOrWhiteSpace(hour))
        {
            errors.Add("Сʱ��ʽ����");
        }
        SetErrors("Hour", errors);
    }

    /// <summary>
    /// ÿ�ܵڼ���
    /// </summary>
    /// <param name="dayOfWeek"></param>
    private void ValidateDayOfWeek(string dayOfWeek)
    {
        var errors = new List<string>(1);
        var pattern = new Regex(@"^(0|1|2|3|4|5|6)(,(0|1|2|3|4|5|6))*$");
        if (!pattern.IsMatch(dayOfWeek) && !string.IsNullOrWhiteSpace(dayOfWeek))
        {
            errors.Add("���ڸ�ʽ����");
        }
        SetErrors("DayOfWeek", errors);
    }

    /// <summary>
    /// ÿ�µĵڼ������֤
    /// </summary>
    /// <param name="dayOfMonth"></param>
    private void ValidateDayOfMonth(string dayOfMonth)
    {
        var errors = new List<string>(1);
        var pattern = new Regex(@"^(31|30|[12][0-9]|1?[1-9])(,(31|30|[12][0-9]|1?[1-9]))*$");
        if (!pattern.IsMatch(dayOfMonth) && !string.IsNullOrWhiteSpace(dayOfMonth))
        {
            errors.Add("���ڸ�ʽ����");
        }
        SetErrors("DayOfMonth", errors);
    }

    /// <summary>
    /// �·���֤
    /// </summary>
    /// <param name="monthlyDay"></param>
    private void ValidateMonthlyDay(string monthlyDay)
    {
        var errors = new List<string>(1);
        var pattern = new Regex(@"^(1|2|3|4|5|6|7|8|9|10|11|12)(,(1|2|3|4|5|6|7|8|9|10|11|12))*$");
        if (!pattern.IsMatch(monthlyDay) && !string.IsNullOrWhiteSpace(monthlyDay))
        {
            errors.Add("�·ݸ�ʽ����");
        }
        SetErrors("MonthlyDay", errors);
    }

    /// <summary>
    /// ��֤CRON���ʽ
    /// </summary>
    /// <param name="cron"></param>
    private void ValidateCron(string cron)
    {
        var errors = new List<string>(1);
        if (!CronExpression.IsValidExpression(cron) && !string.IsNullOrWhiteSpace(cron))
        {
            errors.Add("CRON ���ʽ��ʽ����");
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

    private void CustomTaskSelect_CloseButtonClick(TeachingTip sender, object args)
    {
        ViewModel.SelectedItemChangedCommand.Execute(sender);
    }

    private void CustomGroupTaskSelect_CloseButtonClick(TeachingTip sender, object args)
    {
        ViewModel.SelectGroupItemChangedCommand.Execute(sender);
    }
}
