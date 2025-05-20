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
using System.Diagnostics;
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
        Loaded += RegisterShortcutEvents;
    }

    private void RegisterShortcutEvents(object sender, RoutedEventArgs e)
    {
        foreach (var shortcut in FindVisualChildren<ShortcutControl>(this))
        {
            shortcut.SaveClicked += Shortcut_SaveClicked;
            shortcut.ResetRequested += Shortcut_ResetRequested;
        }
    }

    private void Shortcut_ResetRequested(object sender, EventArgs e)
    {
        if (sender is ShortcutControl shortcut)
        {
            var hotkeyId = shortcut.Parameters;
            shortcut.HotkeySettings.Clear();
            ViewModel.ResetDefaultCommand.Execute(hotkeyId);
            shortcut.HotkeySettings = ViewModel.Hotkeys;
        }
    }

    private void Shortcut_SaveClicked(object sender, Hotkey e)
    {
        ViewModel.RegisterUserDefinedHotkeyCommand.Execute(e);
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
    {
        if (depObj != null)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child != null && child is T t)
                {
                    yield return t;
                }

                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }
    }

}
