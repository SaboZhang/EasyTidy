using System.Configuration;
using System.Data;
using System.Threading;
using System.Windows;

namespace EasyTidy.UpdateLauncher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{

    private readonly string defaultVersion = "1.0.0.0";

    public static string CurrentVersion = "";

    protected override void OnStartup(StartupEventArgs e)
    {
        //阻止多开和用户主动启动
        if (e.Args.Length == 0 || IsAlreadyRunning()) Environment.Exit(0);

        base.OnStartup(e);

        var version = e.Args.FirstOrDefault() ?? defaultVersion;
        CurrentVersion = version;
        // var mainWindow = new MainWindow();
        // mainWindow.Show();
    }

    private bool IsAlreadyRunning()
    {
        mutex = new Mutex(true, "UpdateLauncher", out var isCreatedNew);
        return !isCreatedNew;
    }

    private Mutex? mutex;
}
