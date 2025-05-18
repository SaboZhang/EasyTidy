using EasyTidy.Common.Views;
using EasyTidy.Model;
using EasyTidy.Views.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HotKeySettingPage : ToolPage
{
    public HotKeySettingViewModel ViewModel { get; set; }

    public HotKeySettingPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<HotKeySettingViewModel>();
        DataContext = ViewModel;
        DragWindowShortcut.SaveClicked += DragWindowShortcut_SaveClicked;
        DragWindowShortcut.ResetRequested += DragWindowShortcut_ResetRequested;
    }

    private void DragWindowShortcut_ResetRequested(object sender, EventArgs e)
    {
        DragWindowShortcut.HotkeySettings.Clear();
        var hotkeyId = DragWindowShortcut.Parameters;
        ViewModel.ResetDefaultCommand.Execute(hotkeyId);
        DragWindowShortcut.HotkeySettings = ViewModel.Hotkeys;
    }

    private void DragWindowShortcut_SaveClicked(object sender, ObservableCollection<HotkeysCollection> e)
    {
        ViewModel.RegisterUserDefinedHotkeyCommand.Execute(e);
    }
}
