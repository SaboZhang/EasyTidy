using System.Diagnostics;
using System.IO;
using System.Windows;

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
        string year = DateTime.Now.Year.ToString();
        string datePart = App.CurrentVersion.Substring(App.CurrentVersion.LastIndexOf('.') + 1);
        string month = datePart.Substring(0, 2);
        string day = datePart.Substring(2, 2);
        VersionText.Text = $"新版本号：v{App.CurrentVersion}\n构建日期：{year}年{month}月{day}日";
        Loaded += MainWindow_Loaded;

    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Task task = ProcessDownloadedFile();
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        RetryButton.Visibility = Visibility.Collapsed;
        await ProcessDownloadedFile();
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
        SetStatus("请稍后...");

        await Task.Delay(3000);

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
            RetryButton.Visibility = Visibility.Visible;
        }
    }
}