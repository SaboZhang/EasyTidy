using EasyTidy.Service;
using H.NotifyIcon;
using System.Diagnostics;

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
        App._mutex?.ReleaseMutex();
        App.HandleClosedEvents = false;
        TrayIcon.Dispose();
        App.MainWindow?.Close();
        Environment.Exit(0);
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
        App.MainWindow?.Close();
        // Application.Current.Exit();
        Environment.Exit(0);
    }

    /// <summary>
    /// Stops the file watcher
    /// </summary>
    [RelayCommand]
    private void StopWatcher()
    {
        Settings.AutomaticConfig.RegularTaskRunning = false;
        Settings.Save();
        FileEventHandler.StopAllMonitoring();
        ToastWithAvatar.Instance.ScenarioName = "停止监视";
        ToastWithAvatar.Instance.Description = "EasyTidy停止监视成功";
        ToastWithAvatar.Instance.SendToast();
    }

    [RelayCommand]
    private async Task ExecuteOnceAsync()
    {
        await QuartzHelper.TriggerAllJobsOnceAsync();
    }
}
