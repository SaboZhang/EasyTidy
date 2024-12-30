using EasyTidy.Common.Extensions;
using EasyTidy.Contracts.Service;
using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.Util;
using Windows.UI.ViewManagement;
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
            Logger.Info($"{Constants.AppName}_v{Constants.Version} Closed...\n");
            LogService.UnRegister();
            await QuartzHelper.StopAllJob();
            FileEventHandler.StopAllMonitoring();
        }
    }
}
