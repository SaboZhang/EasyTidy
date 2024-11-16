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
    ///     �°汾����Ŀ¼·��
    /// </summary>
    private readonly string SaveDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);

    /// <summary>
    ///     �°汾��������
    /// </summary>
    private readonly string SaveName = "update.zip";

    public MainWindow()
    {
        this.InitializeComponent();
        Task.Run(() => ProcessDownloadedFile());

    }

    /// <summary>
    ///     �������غõ��°����
    /// </summary>
    /// <returns></returns>
    private async Task ProcessDownloadedFile()
    {
        // ׼������
        var process = Process.GetProcessesByName("EasyTidy");
        if (process != null && process.Length > 0) process[0].Kill();

        SetStatus("������ɣ����ڽ�ѹ����رմ˴���...");

        var unpath = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.FullName;

        var unresult = await Task.Run(async () =>
        {
            await Task.Delay(3000);
            return Unzip.ExtractZipFile(Path.Combine(SaveDir, SaveName), unpath);
        });

        if (unresult)
        {
            SetStatus("������ɣ�", false);
            Process.Start(Path.Combine(unpath, "EasyTidy.exe"));
        }
        else
        {
            SetStatus("��ѹ�ļ�ʱ�����쳣�������ԣ�ͨ�������������Ϊ EasyTidy ��������δ�˳���", false);
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
