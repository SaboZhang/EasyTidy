using EasyTidy.Common.Extensions;
using EasyTidy.Service;
using H.NotifyIcon;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WinUIEx;

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

    public MainViewModel ViewModel { get; }

    public TrayIconView()
    {
        InitializeComponent();
        ViewModel = App.GetService<MainViewModel>();
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
    private void ShowHideWindow() => ViewModel.ToggleChildWindow();
    //private void ShowHideWindow()
    //{
    //    WindowsHelper.EnsureChildWindow();

    //    var childWindow = App.ChildWindow;

    //    SetWindowContent(childWindow);
    //    WindowsHelper.SetWindowStyle(childWindow);
    //    childWindow.SetRequestedTheme(ViewModel.ThemeSelectorService.Theme);

    //    WindowsHelper.PositionWindowBottomRight(childWindow);

    //    childWindow.Activate();
    //}

    /// <summary>
    /// Exits the application
    /// </summary>
    [RelayCommand]
    private void ExitApplication()
    {
        App._mutex?.ReleaseMutex();
        App.HandleClosedEvents = false;
        TrayIcon.Dispose();
        App.ChildWindow?.Close();
        App.MainWindow?.Close();
    }

    /// <summary>
    /// Restarts the application
    /// </summary>
    [RelayCommand]
    private void RestartApplication()
    {
        Logger.Info("Restarting application");
        App._mutex?.ReleaseMutex();
        var appPath = Environment.ProcessPath;
        Process.Start(appPath);
        App.HandleClosedEvents = false;
        TrayIcon.Dispose();
        App.ChildWindow?.Close();
        App.MainWindow?.Close();
        // Environment.Exit(0);
    }

    /// <summary>
    /// Stops the file watcher
    /// </summary>
    [RelayCommand]
    private async Task StopWatcher()
    {
        Settings.AutomaticConfig.RegularTaskRunning = false;
        Settings.Save();
        FileEventHandler.StopAllMonitoring();
        await QuartzHelper.StopAllJob();
    }

    [RelayCommand]
    private async Task ExecuteOnceAsync()
    {
        await QuartzHelper.TriggerAllJobsOnceAsync();
        await ViewModel.ExecuteAllTaskAsync();
    }

    private void SetWindowContent(WindowEx window)
    {
        window.Content = new MainPage();
    }
}
