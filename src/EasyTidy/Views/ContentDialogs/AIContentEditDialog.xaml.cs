using CommunityToolkit.WinUI;
using EasyTidy.Model;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views.ContentDialogs;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AIContentEditDialog : ContentDialog, INotifyDataErrorInfo, INotifyPropertyChanged
{
    public AiSettingsViewModel ViewModel { get; set; }
    public AIContentEditDialog()
    {
        ViewModel = App.GetService<AiSettingsViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
        RequestedTheme = ViewModel.ThemeSelectorService.Theme;
        // ValidateChatModel(_chatModel);
    }

    public string ModelName { get; set; }

    public string Identifier { get; set; }

    public string AppKey { get; set; }

    public string AppID { get; set; }

    public ServiceType ServiceType { get; set; }

    public bool IsEnable { get; set; }

    public string BaseUrl { get; set; }

    private string _chatModel;

    public string ChatModel
    {
        get => _chatModel;
        set
        {
            if (_chatModel != value)
            {
                _chatModel = value;
                OnPropertyChanged();
                ValidateChatModel(value);
                if (_shouldValidate)
                {
                    
                }
            }
        }
    }

    public double Temperature { get; set; } = 0.8;

    private bool _shouldValidate = false;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _shouldValidate = true;
    }

    public void ValidateChatModel(string chatModel)
    {
        var errors = new List<string>(1);
        if (string.IsNullOrWhiteSpace(chatModel))
        {
            errors.Add("ChatModelErrors".GetLocalized());
        }
        SetErrors("ChatModel", errors);
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

    private async void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            args.Cancel = true;
            var result = await ViewModel.VerifyServiceAsync(sender);
            if (result.Item2)
            {
                ShowVerify.Subtitle = result.Item1;
                args.Cancel = false;

            }
            else
            {
                ShowVerify.Subtitle = result.Item1;
            }
        }
        finally
        {
            ShowVerify.IsOpen = true; // 显示验证对话框
            await Task.Delay(3000);
            ShowVerify.IsOpen = false;
        }
    }
}
