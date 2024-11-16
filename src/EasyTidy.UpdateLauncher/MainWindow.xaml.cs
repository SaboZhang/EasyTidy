using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using WinUIEx;
using System.Diagnostics;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.UpdateLauncher;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : WindowEx
{
    /// <summary>
    ///     新版本保存目录路径
    /// </summary>
    private readonly string SaveDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);

    /// <summary>
    ///     新版本保存名字
    /// </summary>
    private readonly string SaveName = "update.zip";

    public MainWindow()
    {
        this.InitializeComponent();
        Task.Run(() => ProcessDownloadedFile());

    }

    /// <summary>
    ///     处理下载好的新版软件
    /// </summary>
    /// <returns></returns>
    private async Task ProcessDownloadedFile()
    {
        // 准备更新
        var process = Process.GetProcessesByName("EasyTidy");
        if (process != null && process.Length > 0) process[0].Kill();

        SetStatus("下载完成，正在解压请勿关闭此窗口...");

        var unpath = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.FullName;

        var unresult = await Task.Run(async () =>
        {
            await Task.Delay(3000);
            return Unzip.ExtractZipFile(Path.Combine(SaveDir, SaveName), unpath);
        });

        if (unresult)
        {
            SetStatus("更新完成！", false);
            Process.Start(Path.Combine(unpath, "EasyTidy.exe"));
        }
        else
        {
            SetStatus("解压文件时发生异常，请重试！通常情况可能是因为 EasyTidy 主程序尚未退出。", false);
        }
    }

    private void SetStatus(string statusText, bool isLoading = true)
    {
        UpdateStatus.Text = statusText;
        UpdateRing.IsActive = isLoading;
        if (isLoading)
            UpdateRing.IsActive = true;
        else
            UpdateRing.IsActive = false;
    }


}
