using H.NotifyIcon;
using System.Windows;

namespace EasyTidy;

public partial class App : Application
{
    public static Mutex _mutex = null;

    public static Window CurrentWindow = Window.Current;
    public IServiceProvider Services { get; }
    public new static App Current => (App)Application.Current;
    public string AppVersion { get; set; } = AssemblyInfoHelper.GetAssemblyVersion();
    public string AppName { get; set; } = "EasyTidy";

    private bool createdNew;

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
        Services = ConfigureServices();
        this.InitializeComponent();
    }

    private static IServiceProvider ConfigureServices()
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

        services.AddTransient<MainViewModel>();
        services.AddTransient<GeneralSettingViewModel>();
        services.AddTransient<ThemeSettingViewModel>();
        services.AddTransient<AppUpdateSettingViewModel>();
        services.AddTransient<AboutUsSettingViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<BreadCrumbBarViewModel>();

        return services.BuildServiceProvider();
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mutex = new Mutex(true, AppName, out createdNew);

        if (!createdNew)
        {
            
            //应用程序已经在运行！当前的执行退出。
            Environment.Exit(0);
        }

        CurrentWindow = new Window();

        CurrentWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        CurrentWindow.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;

        if (CurrentWindow.Content is not Frame rootFrame)
        {
            CurrentWindow.Content = rootFrame = new Frame();
        }

        rootFrame.Navigate(typeof(MainPage));

        CurrentWindow.Title = CurrentWindow.AppWindow.Title = $"{AppName} v{AppVersion}";
        CurrentWindow.AppWindow.SetIcon("Assets/icon.ico");

        CurrentWindow.Closed += (sender, args) =>
        {
            if (HandleClosedEvents)
            {
                args.Handled = true;
                CurrentWindow.Hide();
            }
        };

        CurrentWindow.Activate();
        await DynamicLocalizerHelper.InitializeLocalizer("zh-CN", "en-US");
    }
}

