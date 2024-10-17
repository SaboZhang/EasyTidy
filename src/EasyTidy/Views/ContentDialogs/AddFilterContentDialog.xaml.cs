// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using EasyTidy.Model;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasyTidy.Views.ContentDialogs;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AddFilterContentDialog : ContentDialog, INotifyDataErrorInfo, INotifyPropertyChanged
{
    public FilterViewModel ViewModel { get; set; }

    #region 属性 & 字段
    private string _filterName = string.Empty;
    public string FilterName
    {
        get => _filterName;
        set
        {
            if (_filterName != value)
            {
                _filterName = value;
                OnPropertyChanged();
                ValidateFilterName(_filterName);
            }
        }
    }

    private bool _isSizeSelected = false;
    public bool IsSizeSelected
    {
        get => _isSizeSelected;
        set
        {
            if (_isSizeSelected != value)
            {
                _isSizeSelected = value;
                SizeComboBox.IsEnabled = value;
                SizeTextBox.IsEnabled = value;
                SizeUnitComboBox.IsEnabled = value;
            }
        }
    }
    public ComparisonResult SizeOperator { get; set; }
    public string? SizeValue { get; set; } = null;
    public SizeUnit SizeUnit { get; set; }

    private bool _isCreateDateSelected = false;
    public bool IsCreateDateSelected
    {
        get => _isCreateDateSelected;
        set
        {
            if (_isCreateDateSelected != value)
            {
                _isCreateDateSelected = value;
                CreateDateComboBox.IsEnabled = value;
                CreateDateTextBox.IsEnabled = value;
                CreateDateUnitComboBox.IsEnabled = value;
            }
        }
    }
    public ComparisonResult CreateDateOperator { get; set; }
    public string? CreateDateValue { get; set; } = null;
    public DateUnit CreateDateUnit { get; set; }

    private bool _isEditDateSelected = false;
    public bool IsEditDateSelected
    {
        get => _isEditDateSelected;
        set
        {
            if (_isEditDateSelected != value)
            {
                _isEditDateSelected = value;
                EditDateComboBox.IsEnabled = value;
                EditDateTextBox.IsEnabled = value;
                EditDateUnitComboBox.IsEnabled = value;
            }
        }
    }
    public ComparisonResult EditDateOperator { get; set; }
    public string? EditDateValue { get; set; } = null;
    public DateUnit EditDateUnit { get; set; }

    private bool _isVisitDateSelected = false;
    public bool IsVisitDateSelected
    {
        get => _isVisitDateSelected;
        set
        {
            if (_isVisitDateSelected != value)
            {
                _isVisitDateSelected = value;
                VisitDateComboBox.IsEnabled = value;
                VisitDateTextBox.IsEnabled = value;
                VisitDateUnitComboBox.IsEnabled = value;
            }
        }
    }
    public ComparisonResult VisitDateOperator { get; set; }
    public string? VisitDateValue { get; set; } = null;
    public DateUnit VisitDateUnit { get; set; }

    private bool _isArchiveSelected = false;
    public bool IsArchiveSelected
    {
        get => _isArchiveSelected;
        set
        {
            if (_isArchiveSelected != value)
            {
                _isArchiveSelected = value;
                ArchiveComboBox.IsEnabled = value;
            }
        }
    }
    public YesOrNo ArchiveValue { get; set; }

    private bool _isHiddenSelected = false;
    public bool IsHiddenSelected
    {
        get => _isHiddenSelected;
        set
        {
            if (_isHiddenSelected != value)
            {
                _isHiddenSelected = value;
                HidenComboBox.IsEnabled = value;
            }
        }
    }
    public YesOrNo HiddenValue { get; set; }

    private bool _isReadOnlySelected = false;
    public bool IsReadOnlySelected
    {
        get => _isReadOnlySelected;
        set
        {
            if (_isReadOnlySelected != value)
            {
                _isReadOnlySelected = value;
                RadyOnlyComboBox.IsEnabled = value;
            }
        }
    }
    public YesOrNo ReadOnlyValue { get; set; }

    private bool _isSystemSelected = false;
    public bool IsSystemSelected
    {
        get => _isSystemSelected;
        set
        {
            if (_isSystemSelected != value)
            {
                _isSystemSelected = value;
                SystemComboBox.IsEnabled = value;
            }
        }
    }
    public YesOrNo SystemValue { get; set; }

    private bool _isTempSelected = false;
    public bool IsTempSelected
    {
        get => _isTempSelected;
        set
        {
            if (_isTempSelected != value)
            {
                _isTempSelected = value;
                TempComboBox.IsEnabled = value;
            }
        }
    }
    public YesOrNo TempValue { get; set; }

    private bool _isIncludeSelected = false;
    public bool IsIncludeSelected
    {
        get => _isIncludeSelected;
        set
        {
            if (_isIncludeSelected != value)
            {
                _isIncludeSelected = value;
                OtherTextBox.IsEnabled = value;
            }
        }
    }
    public string IncludedFiles { get; set; }

    private bool _isExcludeSelected = false;
    public bool IsContentSelected
    {
        get => _isExcludeSelected;
        set
        {
            if (_isExcludeSelected != value)
            {
                _isExcludeSelected = value;
                ContentComboBox.IsEnabled = value;
                ContentTextBox.IsEnabled = value;
            }
        }
    }
    public ContentOperatorEnum ContentOperator { get; set; }
    public string ContentValue { get; set; }

    #endregion

    public AddFilterContentDialog()
    {
        ViewModel = App.GetService<FilterViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
        RequestedTheme = ViewModel.themeService.GetElementTheme();
        ValidateFilterName(_filterName);
    }

    #region Validation
    private void ValidateFilterName(string filterName)
    {
        var errors = new List<string>(1);
        if (string.IsNullOrWhiteSpace(filterName))
        {
            errors.Add("过滤器名不能为空");
        }
        SetErrors("FilterName", errors);
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

    #endregion

}
