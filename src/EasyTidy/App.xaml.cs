using CommunityToolkit.WinUI;
using EasyTidy.Activation;
using EasyTidy.Common.Database;
using EasyTidy.Common.Extensions;
using EasyTidy.Contracts.Service;
using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.Util;
using EasyTidy.Util.SettingsInterface;
using H.NotifyIcon;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.Globalization;
using WinUIEx;

namespace EasyTidy;

public partial class App : Application
{
    public static Mutex _mutex = null;

    private readonly DispatcherQueue _dispatcherQueue;

    public static WindowEx MainWindow { get; } = new MainWindow();
    public IServiceProvider Services { get; }
    public static new App Current => (App)Application.Current;
    public string AppVersion { get; set; } = ProcessInfoHelper.Version;
    public IHost Host
    {
        get;
    }
#if DEBUG
    public string AppName { get; set; } = "EasyTidyDev";
#else
    public string AppName { get; set; } = "EasyTidy";
#endif

    private bool _createdNew;

    public static bool HandleClosedEvents { get; set; } = true;

    public static UIElement? AppTitlebar { get; set; }

    public static T GetService<T>() where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
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
        UnhandledException += App_UnhandledException;

        if (!RuntimeHelper.IsMSIX)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
        }
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        // 加载配置
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices((context, services) =>
            {
                services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();
                services.AddTransient<IActivationHandler, AppNotificationActivationHandler>();
                // Register Core Services
                services.AddSingleton<ISettingsManager, SettingsHelper>();
                services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
                services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
                services.AddTransient<INavigationViewService, NavigationViewService>();
                services.AddSingleton<IActivationService, ActivationService>();
                services.AddSingleton<IPageService, PageService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IAppNotificationService, AppNotificationService>();

                // Register File Service
                services.AddSingleton<IFileService, FileService>();

                services.AddSingleton(_ => MainWindow);
                services.AddSingleton(_ => MainWindow.DispatcherQueue);

                // Register ViewModels
                services.AddTransient<GeneralViewModel>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<GeneralSettingViewModel>();
                services.AddTransient<ThemeSettingViewModel>();
                services.AddTransient<AppUpdateSettingViewModel>();
                services.AddTransient<AboutUsSettingViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<AutomaticViewModel>();
                services.AddTransient<TaskOrchestrationViewModel>();
                services.AddTransient<FilterViewModel>();
                services.AddTransient<ShellPage>();
                services.AddTransient<ShellViewModel>();

                // Register AppDbContext
                services.AddDbContext<AppDbContext>(options => 
                options.UseSqlite($"Data Source={Path.Combine(Constants.CnfPath, "EasyTidy.db")}"), ServiceLifetime.Scoped);

                // Configuration
                services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));

            })
            .Build();

        App.GetService<IAppNotificationService>().Initialize();
        this.InitializeComponent();
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

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        InitializeLogging();
        if (!IsSingleInstance())
        {
            App.GetService<IAppNotificationService>().Show("RepeatedStartup".GetLocalized());
            Environment.Exit(0);
        }
        App.GetService<IAppNotificationService>().Show(string.Format("AppNotificationPayload".GetLocalized(), Constants.Version));
        await App.GetService<IActivationService>().ActivateAsync(args);
        SetApplicationLanguage();

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

