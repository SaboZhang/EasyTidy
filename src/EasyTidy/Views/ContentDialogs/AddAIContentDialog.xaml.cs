using EasyTidy.Model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views.ContentDialogs;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AddAIContentDialog : ContentDialog, INotifyPropertyChanged, INotifyDataErrorInfo
{
    public event PropertyChangedEventHandler PropertyChanged;
    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

    private readonly Dictionary<string, List<string>> _errors = new();
    public AiSettingsViewModel ViewModel { get; set; }
    public AddAIContentDialog()
    {
        ViewModel = App.GetService<AiSettingsViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
        RequestedTheme = ViewModel.ThemeSelectorService.Theme;
    }

    public string ModelName { get; set; }

    public string Identifier { get; set; }

    public string AppKey { get; set; }

    public string AppID { get; set; }

    public ServiceType ServiceType { get; set; }

    public bool IsEnable { get; set; }

    public string BaseUrl { get; set; }

    private string _chatModel;

    [Required(ErrorMessage = "模型名称不能为空")]
    [StringLength(50, ErrorMessage = "模型名称不能超过50个字符")]
    public string ChatModel
    {
        get => _chatModel;
        set
        {
            if (_chatModel != value)
            {
                _chatModel = value;
                OnPropertyChanged(nameof(ChatModel));
                ValidateProperty(value, nameof(ChatModel));
            }
        }
    }

    public double Temperature { get; set; } = 0.8;

    public string ErrorMessage { get; set; } = string.Empty;

    // 实现 INotifyDataErrorInfo 接口
    public bool HasErrors => _errors.Any();

    public IEnumerable GetErrors(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return null;

        return _errors.ContainsKey(propertyName) ? _errors[propertyName] : null;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    public void ValidateProperty(object value, string propertyName)
    {
        var validationContext = new ValidationContext(this)
        {
            MemberName = propertyName
        };
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateProperty(value, validationContext, results))
        {
            _errors[propertyName] = results.Select(r => r.ErrorMessage).ToList();
            foreach (var error in _errors[propertyName])
            {
                ChatModelError.Text = error;
                ChatModelError.Visibility = Visibility.Visible;
            }
        }
        else
        {
            _errors.Remove(propertyName);
            ChatModelError.Visibility = Visibility.Collapsed;
        }

        OnErrorsChanged(propertyName);
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
