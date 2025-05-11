using EasyTidy.Common.Extensions;
using EasyTidy.Service;
using H.NotifyIcon;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
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
    private void ShowHideWindow()
    {
        // 如果窗口已经关闭，则重新创建
        if (App.ChildWindow == null || App.ChildWindow.IsClosed())
        {
            App.ChildWindow = new WindowEx(); // 重新创建窗口
        }
        App.ChildWindow.Title = "EasyTidy";
        App.ChildWindow.ExtendsContentIntoTitleBar = true;
        var childWindow = App.ChildWindow;
        var subPage = new MainPage();
        childWindow.Content = subPage; // 使用你的自定义控件作为窗口内容
        childWindow.MaxHeight = 340;
        childWindow.MaxWidth = 300;
        childWindow.MoveAndResize(1800, 900, 300, 340);
        childWindow.IsMaximizable = false;
        childWindow.SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.Base };
        childWindow.SetRequestedTheme(ViewModel.ThemeSelectorService.Theme);
        childWindow.Activate();
    }

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
}
