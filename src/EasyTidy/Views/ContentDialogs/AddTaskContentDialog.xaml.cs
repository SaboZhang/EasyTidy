// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasyTidy.Views.ContentDialogs;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AddTaskContentDialog : ContentDialog, INotifyDataErrorInfo, INotifyPropertyChanged
{
    public FileExplorerViewModel ViewModel { get; set; }

    private string _groupName;
    public string GroupName
    {
        get => _groupName;
        set
        {
            if (_groupName != value)
            {
                _groupName = value;
                ValidateGroupName(_groupName);
                OnPropertyChanged();
            }
        }
    }

    public string TaskName { get; set; }

    public string TaskRule { get; set; }

    public string TaskSource { get; set; }

    public bool Shortcut { get; set; }

    public string TaskTarget { get; set; }

    public bool EnabledFlag { get; set; } = true;

    public AddTaskContentDialog()
    {
        ViewModel = App.GetService<FileExplorerViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
        RequestedTheme = ViewModel.themeService.GetElementTheme();
    }

    private void ValidateGroupName(string groupName)
    {
        var errors = new List<string>(1);
        if (string.IsNullOrWhiteSpace(groupName))
        {
            errors.Add("组名不能为空");
        }
        SetErrors("GroupName", errors);
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
}
