using CommunityToolkit.WinUI;
using EasyTidy.Log;
using H.NotifyIcon;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.Globalization;

namespace EasyTidy;

public partial class App : Application
{
    public static Mutex _mutex = null;

    public static Window MainWindow = Window.Current;
    public IServiceProvider Services { get; }
    public new static App Current => (App)Application.Current;
    public string AppVersion { get; set; } = AssemblyInfoHelper.GetAssemblyVersion();
    public string AppName { get; set; } = "EasyTidy";

    private bool createdNew;

    private NotificationManager notificationManager;

    public static bool HandleClosedEvents { get; set; } = true;

    public static T GetService<T>() where T : class
    {
        if ((App.Current as App)!.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public App()
    {
        if (!PackageHelper.IsPackaged)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            var c_notificationHandlers = new Dictionary<int, Action<AppNotificationActivatedEventArgs>>
            {
                { ToastWithAvatar.Instance.ScenarioId, ToastWithAvatar.Instance.NotificationReceived }
            };
            notificationManager = new NotificationManager(c_notificationHandlers);
        }
        Services = ConfigureServices();
        this.InitializeComponent();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IJsonNavigationViewService>(factory =>
        {
            var json = new JsonNavigationViewService();
            json.ConfigDefaultPage(typeof(GeneralPage));
            json.ConfigSettingsPage(typeof(SettingsPage));
            return json;
        });

        services.AddTransient<GeneralViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<GeneralSettingViewModel>();
        services.AddTransient<ThemeSettingViewModel>();
        services.AddTransient<AppUpdateSettingViewModel>();
        services.AddTransient<AboutUsSettingViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<BreadCrumbBarViewModel>();
        services.AddTransient<AutomaticViewModel>();
        services.AddTransient<TaskOrchestrationViewModel>();
        services.AddTransient<FilterViewModel>();

        return services.BuildServiceProvider();
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (!string.IsNullOrEmpty(Settings.Language))
        {
            Logger.Info($"当前语言被设置为：{Settings.Language}");
            ApplicationLanguages.PrimaryLanguageOverride = Settings.Language;
        }
        // 开启日志服务
        LogService.Register("", LogLevel.Debug, AppVersion);

        if (!PackageHelper.IsPackaged)
        {
            notificationManager.Init(notificationManager, OnNotificationInvoked);
        }
        _mutex = new Mutex(true, AppName, out createdNew);

        if (!createdNew)
        {
            ToastWithAvatar.Instance.Description = "ToastWithAvatarDescriptionText".GetLocalized();
            ToastWithAvatar.Instance.ScenarioName = "RemindText".GetLocalized();
            //应用程序已经在运行！当前的执行退出。
            ToastWithAvatar.Instance.SendToast();
            Environment.Exit(0);
        }

        MainWindow = new Window();

        MainWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        MainWindow.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;

        if (MainWindow.Content is not Frame rootFrame)
        {
            MainWindow.Content = rootFrame = new Frame();
        }

        rootFrame.Navigate(typeof(MainPage));

        MainWindow.Title = MainWindow.AppWindow.Title = $"{AppName} v{AppVersion}";
        MainWindow.AppWindow.SetIcon("Assets/icon.ico");

        MainWindow.Closed += (sender, args) =>
        {
            if (HandleClosedEvents)
            {
                args.Handled = true;
                MainWindow.Hide();
            }
        };

        MainWindow.Activate();
        if ((bool)Settings.GeneralConfig.IsStartupCheck)
        {
            try
            {
                Settings.LastUpdateCheck = DateTime.Now.ToShortDateString();
                var update = await UpdateHelper.CheckUpdateAsync("SaboZhang", "Organize", new Version(Current.AppVersion));
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }
        // await DynamicLocalizerHelper.InitializeLocalizer("zh-CN", "en-US");
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        // 记录日志
        if (Logger != null && !HandleClosedEvents)
        {
            Logger.Info($"{AppName}_{AppVersion} Closed...\n");
            LogService.UnRegister();
        }
    }

    private void OnNotificationInvoked(string message)
    {
        // 记录日志
        Logger.Info($"Notification Invoked: {message}");
    }

    void OnProcessExit(object sender, EventArgs e)
    {
        notificationManager.Unregister();
    }
}

