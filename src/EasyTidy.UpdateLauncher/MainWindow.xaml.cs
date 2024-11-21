using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EasyTidy.UpdateLauncher;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    ///    新版本保存目录路径
    /// </summary>
    private readonly string SaveDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory);

    /// <summary>
    ///   新版本保存名字
    /// </summary>
    private readonly string SaveName = "update.zip";

    public MainWindow()
    {
        InitializeComponent();
        VersionText.Text = $"当前版本号：v{App.CurrentVersion}";
        Loaded += MainWindow_Loaded;
        
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Task task = ProcessDownloadedFile();
    }

    /// <summary>
    ///     处理下载好的新版软件
    /// </summary>
    /// <returns></returns>
    private async Task ProcessDownloadedFile()
    {
        // 处理更新
        var process = Process.GetProcessesByName("EasyTidy");
        // if (process != null && process.Length > 0) process[0].Kill();

        if (process != null && process.Length > 0)
        {
            try
            {
                process[0].Kill();
                process[0].WaitForExit(5000); // 等待进程终止（可选，超时为5秒）
            }
            catch (Exception ex)
            {
                SetStatus($"终止进程时发生错误：{ex.Message}", false);
            }
        }

        SetStatus("正在解压，请勿关闭此窗口...");

        var unpath = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.FullName;

        var unresult = await Task.Run(async () =>
        {
            await Task.Delay(3000);
            return Unzip.ExtractZipFile(System.IO.Path.Combine(SaveDir, SaveName), unpath);
        });

        if (unresult)
        {
            SetStatus("更新完成！", false);
            Process.Start(System.IO.Path.Combine(unpath, "EasyTidy.exe"));
        }
        else
        {
            SetStatus("解压文件时发生异常，请重试！通常情况可能是因为 EasyTidy 主程序尚未退出。", false);
        }
    }

    private void SetStatus(string statusText, bool isLoading = true)
    {
        UpdateStatus.Text = statusText;
        UpdateRing.IsIndeterminate = isLoading;
        if (isLoading)
        {
            UpdateRing.IsIndeterminate = true;
            UpdateRing.Visibility = Visibility.Visible;
        }
        else
        {
            UpdateRing.IsIndeterminate = false;
            UpdateRing.Visibility = Visibility.Collapsed;
        }  
    }
}