using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Service;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using Windows.UI.ViewManagement;
using WinRT.Interop;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : WindowEx
{

    private Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;

    private UISettings _settings;

    [StructLayout(LayoutKind.Sequential)]
    struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    [DllImport("dwmapi")]
    static extern IntPtr DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

    public MainWindow()
    {
        this.InitializeComponent();
        Content = null;
        Title = Constants.AppName;

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _settings = new UISettings();
        _settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event
        if (!RuntimeHelper.IsMSIX)
        {
            this.Closed += OnProcessExit;
            SystemEvents.SessionEnding += OnSessionEnding;
        }
        if (IsWindows10())
        {
            var handle = WindowNative.GetWindowHandle(this);
            var margins = new MARGINS { cxLeftWidth = 0, cxRightWidth = 0, cyBottomHeight = 0, cyTopHeight = 2 };
            Activated +=
              (object sender, WindowActivatedEventArgs args) => DwmExtendFrameIntoClientArea(handle, ref margins);
        }

    }

    // this handles updating the caption button colors correctly when indows system theme is changed
    // while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        _dispatcherQueue.TryEnqueue(() =>
        {
            TitleBarHelper.ApplySystemThemeToCaptionButtons();
        });
    }

    async void OnProcessExit(object sender, WindowEventArgs e)
    {
        if (Logger != null && !App.HandleClosedEvents)
        {
            await SaveAppState();
            await QuartzHelper.StopAllJob();
            FileEventHandler.StopAllMonitoring();
            var hotkeyService = App.GetService<HotkeyService>();
            hotkeyService.Clear();
            Logger.Info($"{Constants.AppName}_v{Constants.Version} Closed...\n");
            LogService.UnRegister();
        }
    }

    private async void OnSessionEnding(object sender, EventArgs e)
    {
        // 系统即将关闭或用户注销时触发
        await SaveAppState();
    }

    private async Task SaveAppState()
    {
        if (Settings.AutomaticConfig.IsShutdownExecution)
        {
            await ShutdownService.OnShutdownAsync();
            Logger.Info("退出执行成功！");
        }
    }

    private static bool IsWindows10()
    {
        return Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Minor < 22000;
    }
}
