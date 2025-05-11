using EasyTidy.Model;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using WinUIEx;

namespace EasyTidy.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        this.InitializeComponent();
    }

    private void PinBtn_Click(object sender, RoutedEventArgs e)
    {
        var top = App.ChildWindow.GetIsAlwaysOnTop();
        if (top)
        {
            App.ChildWindow.SetIsAlwaysOnTop(false);
        }
        else
        {
            App.ChildWindow.SetIsAlwaysOnTop(true);
        }
    }

    private void ContentArea_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Move;
    }

    private async void ContentArea_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
            return;

        var droppedItems = await e.DataView.GetStorageItemsAsync();
        List<string> filePaths = new();

        foreach (var item in droppedItems)
        {
            if (item is StorageFile file)
            {
                filePaths.Add(file.Path);
            }
            else if (item is StorageFolder folder)
            {
                await AddFilesFromFolder(folder, filePaths);
            }
        }

        if (filePaths.Count == 0)
        {
            Logger.Warn("未检测到可用的文件");
            return;
        }

        // 获取选中的任务并处理文件
        if (DefaultFileMoveRules.SelectedItem is TaskOrchestrationTable task)
        {
            Logger.Info($"已拖入 {filePaths.Count} 个文件");
            _ = ProcessDroppedFiles(task, filePaths);
        }
        else
        {
            Logger.Warn("未选择有效的任务，无法处理文件");
        }
    }

    /// <summary>
    /// 递归获取文件夹中的所有文件
    /// </summary>
    private async Task AddFilesFromFolder(StorageFolder folder, List<string> filePaths)
    {
        var files = await folder.GetFilesAsync();
        foreach (var file in files)
        {
            filePaths.Add(file.Path);
        }

        var subfolders = await folder.GetFoldersAsync();
        foreach (var subfolder in subfolders)
        {
            await AddFilesFromFolder(subfolder, filePaths);
        }
    }

    /// <summary>
    /// 处理拖拽进来的文件
    /// </summary>
    private async Task ProcessDroppedFiles(TaskOrchestrationTable task, List<string> filePaths)
    {
        var noMatchFiles = new StringBuilder();
        foreach (var filePath in filePaths)
        {
            Logger.Info($"任务 {task.TaskName} 处理文件: {filePath}");
            // 在这里执行文件移动、复制等操作
            var noMatch = await ViewModel.ExecuteTaskAsync(task, filePath);
            if (!string.IsNullOrEmpty(noMatch))
            {
                noMatchFiles.AppendLine(noMatch);
            }
        }
        string noMatchResult = noMatchFiles.ToString();

        if (!string.IsNullOrEmpty(noMatchResult))
        {
            Logger.Warn($"以下文件未匹配任何规则:\n{noMatchResult}");
            ShowNoMatchDialog($"以下文件未匹配任何规则:\n{noMatchResult}");
        }
    }

    private async void ShowNoMatchDialog(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "未匹配规则的文件",
            Content = message,
            CloseButtonText = "确定",
            XamlRoot = App.ChildWindow.Content.XamlRoot, // 适用于 WinUI 3
            RequestedTheme = ViewModel.ThemeSelectorService.Theme
        };

        await dialog.ShowAsync();
    }

}

