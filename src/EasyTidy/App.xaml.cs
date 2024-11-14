using CommunityToolkit.WinUI;
using EasyTidy.Common.Database;
using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Service;
using H.NotifyIcon;
using Microsoft.EntityFrameworkCore;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.Globalization;

namespace EasyTidy;

public partial class App : Application
{
    public static Mutex _mutex = null;

    public static Window MainWindow = Window.Current;
    public IServiceProvider Services { get; }
    public new static App Current => (App)Application.Current;
    public string AppVersion { get; set; } = ProcessInfoHelper.Version;

    public IJsonNavigationViewService GetJsonNavigationViewService => GetService<IJsonNavigationViewService>();
    public string AppName { get; set; } = "EasyTidy";

    private bool _createdNew;

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
        // 注册全局异常处理
        AppDomain.CurrentDomain.UnhandledException += GlobalExceptionHandler;
        // 注册应用程序的未处理异常事件
        Application.Current.UnhandledException += App_UnhandledException;

        if (!PackageHelper.IsPackaged)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
        }
        Services = ConfigureServices();
        this.InitializeComponent();
        _dbContext = Services.GetRequiredService<AppDbContext>();
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        LogException(e.Exception);
        e.Handled = true;
    }

    private void GlobalExceptionHandler(object sender, System.UnhandledExceptionEventArgs e)
    {
        Exception ex = (Exception)e.ExceptionObject;
        LogException(ex);
    }

    private void LogException(Exception ex)
    {
        // 记录异常的逻辑
        Logger.Error($"Global exception caught: {ex}");
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
            ServiceLifetime.Scoped);

        var serviceProvider = services.BuildServiceProvider();
        InitializeDatabase(serviceProvider);

        return serviceProvider;
    }

    private static void InitializeDatabase(ServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.InitializeDatabaseAsync().GetAwaiter().GetResult();
        }
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        InitializeLogging();
        SetApplicationLanguage();

        if (!IsSingleInstance())
        {
            Environment.Exit(0);
        }

        InitializeMainWindow();
        SetWindowBehavior();
        await PerformStartupChecksAsync();

        Logger.Fatal("EasyTidy Initialized Successfully!");
    }

    private void InitializeLogging()
    {
#if !DEBUG
        LogService.Register("", LogLevel.Error, AppVersion);
#else
        LogService.Register("", LogLevel.Debug, AppVersion);
#endif
    }

    private void SetApplicationLanguage()
    {
        if (!string.IsNullOrEmpty(Settings.Language))
        {
            Logger.Info($"当前语言被设置为：{Settings.Language}");
            ApplicationLanguages.PrimaryLanguageOverride = Settings.Language;
        }
    }

    private bool IsSingleInstance()
    {
        if (!Settings.GeneralConfig.EnableMultiInstance) 
        {
            _mutex = new Mutex(true, AppName, out _createdNew);
            return _createdNew;
        }
        _mutex = new Mutex(false, AppName, out _createdNew);
        return true;
    }

    private void InitializeMainWindow()
    {
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
    }

    private void SetWindowBehavior()
    {
        MainWindow.Closed += (sender, args) =>
        {
            if (HandleClosedEvents)
            {
                args.Handled = true;
                MainWindow.Hide();
            }
        };

        if ((bool)Settings.GeneralConfig.Minimize)
        {
            // MainWindow.Activate();
            MainWindow.Hide();
        }
        else
        {
            MainWindow.Activate();
        }
    }

    private async Task PerformStartupChecksAsync()
    {
        if ((bool)Settings.GeneralConfig.IsStartupCheck)
        {
            var app = new AppUpdateSettingViewModel();
            await app.CheckForNewVersionAsync();
        }

        await QuartzConfig.InitQuartzConfigAsync();
        await QuartzHelper.StartAllJob();
    }

    private void OnNotificationInvoked(string message)
    {
        // 记录日志
        Logger.Info($"Notification Invoked: {message}");
    }

    async void OnProcessExit(object sender, EventArgs e)
    {
        // 记录日志
        if (Logger != null && !HandleClosedEvents)
        {
            Logger.Info($"{AppName}_{AppVersion} Closed...\n");
            LogService.UnRegister();
        }
        await QuartzHelper.StopAllJob();
        FileEventHandler.StopAllMonitoring();
    }

}

