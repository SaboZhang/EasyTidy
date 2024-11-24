using CommunityToolkit.WinUI;
using EasyTidy.Activation;
using EasyTidy.Common.Database;
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
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.Globalization;
using WinUIEx;

namespace EasyTidy;

public partial class App : Application
{
    public static Mutex _mutex = null;

    public static WindowEx MainWindow { get; } = new MainWindow();
    public IServiceProvider Services { get; }
    public static new App Current => (App)Application.Current;
    public string AppVersion { get; set; } = ProcessInfoHelper.Version;
    public IHost Host
    {
        get;
    }
    public string AppName { get; set; } = "EasyTidy";

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
        Application.Current.UnhandledException += App_UnhandledException;

        if (!RuntimeHelper.IsMSIX)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
        }
        // 加载配置
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices((context, services) =>
            {
                services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();
                // Register Core Services
                // services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<ISettingsManager, SettingsHelper>();
                services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
                services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
                services.AddTransient<INavigationViewService, NavigationViewService>();
                services.AddSingleton<IActivationService, ActivationService>();
                services.AddSingleton<IPageService, PageService>();
                services.AddSingleton<INavigationService, NavigationService>();

                // Register ServiceConfig
                services.AddSingleton<ServiceConfig>(factory =>
                {
                    var settingsManager = factory.GetRequiredService<ISettingsManager>();
                    var serviceConfig = new ServiceConfig(settingsManager);
                    serviceConfig.SetConfigModel();
                    return serviceConfig;
                });

                // Register File Service
                services.AddSingleton<IFileService, FileService>();

                // Register ViewModels
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
                services.AddTransient<ShellPage>();
                services.AddTransient<ShellViewModel>();

                // Register AppDbContext
                services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={Path.Combine(Constants.CnfPath, "EasyTidy.db")}"), ServiceLifetime.Scoped);

                // Configuration
                services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));

                // Initialize Database
                InitializeDatabase(services);
            })
            .Build();
        //var configuration = new ConfigurationBuilder()
        //    .Build();
        // Services = ConfigureServices(configuration);
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

    //private static ServiceProvider ConfigureServices(IConfiguration configuration)
    //{
    //    var services = new ServiceCollection();
    //    services.AddSingleton<IThemeService, ThemeService>();
    //    services.AddSingleton<ISettingsManager, SettingsHelper>();
    //    services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
    //    services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
    //    services.AddTransient<INavigationViewService, NavigationViewService>();
    //    services.AddSingleton<IPageService, PageService>();
    //    services.AddSingleton<INavigationService, NavigationService>();
    //    services.AddSingleton<ServiceConfig>(factory => {

    //        var settingsManager = factory.GetRequiredService<ISettingsManager>();
    //        var serviceConfig = new ServiceConfig(settingsManager);
    //        serviceConfig.SetConfigModel();
    //        return serviceConfig;
    //    });
    //    //services.AddSingleton<IJsonNavigationViewService>(factory =>
    //    //{
    //    //    var json = new JsonNavigationViewService();
    //    //    json.ConfigDefaultPage(typeof(GeneralPage));
    //    //    json.ConfigSettingsPage(typeof(SettingsPage));
    //    //    return json;
    //    //});

    //    services.AddSingleton<IFileService, FileService>();

    //    services.AddTransient<GeneralViewModel>();
    //    services.AddTransient<MainViewModel>();
    //    services.AddTransient<GeneralSettingViewModel>();
    //    services.AddTransient<ThemeSettingViewModel>();
    //    services.AddTransient<AppUpdateSettingViewModel>();
    //    services.AddTransient<AboutUsSettingViewModel>();
    //    services.AddTransient<SettingsViewModel>();
    //    services.AddTransient<BreadCrumbBarViewModel>();
    //    services.AddTransient<AutomaticViewModel>();
    //    services.AddTransient<TaskOrchestrationViewModel>();
    //    services.AddTransient<FilterViewModel>();

    //    // 注册 AppDbContext
    //    services.AddDbContext<AppDbContext>(options =>
    //        options.UseSqlite($"Data Source={Path.Combine(Constants.CnfPath, "EasyTidy.db")}"),
    //        ServiceLifetime.Scoped);

    //    services.Configure<LocalSettingsOptions>(configuration.GetSection(nameof(LocalSettingsOptions)));

    //    var serviceProvider = services.BuildServiceProvider();
    //    InitializeDatabase(serviceProvider);

    //    return serviceProvider;
    //}

    private static void InitializeDatabase(ServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.InitializeDatabaseAsync().GetAwaiter().GetResult();
        }
    }

    private static void InitializeDatabase(IServiceCollection services)
    {
        using (var serviceProvider = services.BuildServiceProvider())
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.InitializeDatabaseAsync().GetAwaiter().GetResult();
            }
        }
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        InitializeLogging();
        await App.GetService<IActivationService>().ActivateAsync(args);
        SetApplicationLanguage();

        if (!IsSingleInstance())
        {
            Environment.Exit(0);
        }

        SetWindowBehavior();
        // await PerformStartupChecksAsync();

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

