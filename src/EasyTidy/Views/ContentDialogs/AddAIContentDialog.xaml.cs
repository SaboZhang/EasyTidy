using EasyTidy.Model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views.ContentDialogs;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AddAIContentDialog : ContentDialog
{
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

    public string ChatModel { get; set; }

    public double Temperature { get; set; } = 0.8;

}
