using CommunityToolkit.WinUI;
using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.Util;
using H.NotifyIcon;
using Microsoft.EntityFrameworkCore;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.Globalization;
using Quartz;
using System.Collections.Specialized;

namespace EasyTidy;

public partial class App : Application
{
    public static Mutex _mutex = null;

    public static Window MainWindow = Window.Current;
    public IServiceProvider Services { get; }
    public new static App Current => (App)Application.Current;
    public string AppVersion { get; set; } = AssemblyInfoHelper.GetAssemblyVersion();
    public string AppName { get; set; } = "EasyTidy";

    private bool _createdNew;

    private NotificationManager _notificationManager;

    public static bool HandleClosedEvents { get; set; } = true;

    private readonly AppDbContext _dbContext;

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
            _notificationManager = new NotificationManager(c_notificationHandlers);
        }
        Services = ConfigureServices();
        this.InitializeComponent();
        _dbContext = Services.GetRequiredService<AppDbContext>();
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

        // 注册 AppDbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={Path.Combine(Constants.CnfPath, "EasyTidy.db")}"),
            ServiceLifetime.Singleton); // 使用 Singleton 确保唯一实例

        return services.BuildServiceProvider();
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // 开启日志服务
        LogService.Register("", LogLevel.Debug, AppVersion);

        if (!string.IsNullOrEmpty(Settings.Language))
        {
            Logger.Info($"当前语言被设置为：{Settings.Language}");
            ApplicationLanguages.PrimaryLanguageOverride = Settings.Language;
        }

        if (!PackageHelper.IsPackaged)
        {
            _notificationManager.Init(_notificationManager, OnNotificationInvoked);
        }
        _mutex = new Mutex(true, AppName, out _createdNew);

        if (!_createdNew)
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

        await QuartzConfig.InitQuartzConfigAsync();
        await QuartzHelper.InitAsync();
        await QuartzHelper.StartAllJob();

    }

    private void OnNotificationInvoked(string message)
    {
        // 记录日志
        Logger.Info($"Notification Invoked: {message}");
    }

    void OnProcessExit(object sender, EventArgs e)
    {
        // 记录日志
        if (Logger != null && !HandleClosedEvents)
        {
            Logger.Info($"{AppName}_{AppVersion} Closed...\n");
            LogService.UnRegister();
        }
        _notificationManager.Unregister();
    }

}

