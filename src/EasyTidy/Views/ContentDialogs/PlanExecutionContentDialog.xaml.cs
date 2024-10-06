// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Newtonsoft.Json.Linq;
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
    public IThemeService ThemeService { get; set; }

    public AutomaticViewModel ViewModel { get; set; }

    public string MonthlyDay { get; set; }

    public string DayOfMonth { get; set; }

    public string DayOfWeek { get; set; }

    public string Hour { get; set; }

    // public string Minute { get; set; }
    private string _minute;

    public string Minute
    {
        get => _minute;
        set
        {
            if (_minute != value)
            {
                _minute = value;
                OnPropertyChanged();
            }
        }
    }

    // public string CronExpression { get; set;}
    private string _cronExpression;

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

    public bool HasErrors => _validationErrors.Count > 0;

    public PlanExecutionContentDialog()
    {
        ViewModel = App.GetService<AutomaticViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
        RequestedTheme = ViewModel.themeService.GetElementTheme();
    }


    private void ValidateCron(string cron)
    {
        string pattern = @"/(((\d+,)+\d+|(\d+(\/|-)\d+)|\d+|\*) ?){5,7}/";
        var errors = new List<string>(1);
        if (!Regex.IsMatch(cron, pattern))
        {
            errors.Add("CRON ±í´ïÊ½´íÎó");
        }
        SetErrors("CronExpression", errors);
    }

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
}
