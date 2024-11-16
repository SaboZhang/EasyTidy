using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using WinUIEx;
using System.Threading;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.UpdateLauncher;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        RequestedTheme = ApplicationTheme.Dark;
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        //if (args.Arguments != "EasyTidy" || IsAlreadyRunning())
        //{
        //    // Exit the application if not launched by EasyTidy
        //    Application.Current.Exit();
        //    return;
        //}
        m_window = new MainWindow();
        m_window.SetIcon("Assets/update.ico");
        // Hide system title bar.
        m_window.ExtendsContentIntoTitleBar = true;
        m_window.Activate();
    }

    /// <summary>
    ///     获取当前程序是否已运行
    /// </summary>
    private bool IsAlreadyRunning()
    {
        mutex = new Mutex(true, "EasyTidy", out var isCreatedNew);
        return !isCreatedNew;
    }

    public Window m_window;

    private Mutex? mutex;

}
