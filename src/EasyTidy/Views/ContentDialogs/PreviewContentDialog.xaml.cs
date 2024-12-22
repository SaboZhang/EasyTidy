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

namespace EasyTidy.Views.ContentDialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PreviewContentDialog : ContentDialog
    {
        public AutomaticViewModel ViewModel { get; set; }

        public PreviewContentDialog()
        {
            ViewModel = App.GetService<AutomaticViewModel>();
            this.InitializeComponent();
            XamlRoot = App.MainWindow.Content.XamlRoot;
            RequestedTheme = ViewModel.ThemeSelectorService.Theme;
        }

        public string TaskName { get; set; }
        public string TaskGroup { get; set; }
        public string OperatingMethod { get; set; }
        public string Rules { get; set; }
        public string FileFlow { get; set; }
        public string AutomatedRules { get; set; }
        public string FilterId { get; set; }
    }
}
