using EasyTidy.Service;
using H.NotifyIcon;
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
    public HotKeySettingViewModel HotKey { get; }

    public string ToggleHotkeyText
    {
        get
        {
            return HotKey.IsHotkeyEnabled
                ? "禁用热键"
                : "启用热键";
        }
    }

    public static TrayIconView Instance { get; private set; }

    public TrayIconView()
    {
        InitializeComponent();
        ViewModel = App.GetService<MainViewModel>();
        HotKey = App.GetService<HotKeySettingViewModel>();
        Instance = this;
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

    public void DisposeTrayIcon()
    {
        TrayIcon.Dispose();
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

    [RelayCommand]
    private async Task DisableHotkeysAsync()
    {
        await HotKey.DisableHotkeysAsync();
        OnPropertyChanged(nameof(ToggleHotkeyText));
    }
}
