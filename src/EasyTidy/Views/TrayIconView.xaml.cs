using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>

[ObservableObject]
public sealed partial class TrayIconView : UserControl
{
    [ObservableProperty]
    private bool _isWindowVisible;

    public TrayIconView()
    {
        InitializeComponent();
    }

    [RelayCommand]
    private void ShowSettings()
    {
        var window = App.MainWindow;
        if (window == null)
        {
            return;
        }
        window.Show();
        IsWindowVisible = window.Visible;
    }

    /// <summary>
    /// Shows or hides the main window
    /// </summary>
    [RelayCommand]
    private void ShowHideWindow()
    {
        var window = App.MainWindow;
        if (window == null)
        {
            return;
        }

        if (window.Visible)
        {
            window.Hide();
        }
        else
        {
            window.Show();
        }
        IsWindowVisible = window.Visible;
    }

    /// <summary>
    /// Exits the application
    /// </summary>
    [RelayCommand]
    private void ExitApplication()
    {
        App.HandleClosedEvents = false;
        TrayIcon.Dispose();
        App.MainWindow?.Close();
    }

    /// <summary>
    /// Restarts the application
    /// </summary>
    [RelayCommand]
    private void RestartApplication()
    {
        Logger.Info("Restarting application");
        App._mutex.ReleaseMutex();
        var appPath = Environment.ProcessPath;
        Process.Start(appPath);
        App.HandleClosedEvents = false;
        TrayIcon.Dispose();
        App.MainWindow?.Close();
    }
}
