using CommunityToolkit.WinUI.UI;
using EasyTidy.Model;
using EasyTidy.Util;
using EasyTidy.Views.ContentDialogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.ViewModels;

public partial class FileExplorerViewModel : ObservableRecipient
{
    public IThemeService themeService;

    public FileExplorerViewModel(IThemeService themeService)
    {
        this.themeService = themeService;
    }

    public FileExplorerViewModel()
    {
        _ = OnPageLoaded();
    }

    [ObservableProperty]
    private IList<OperationMode> operationModes = Enum.GetValues(typeof(OperationMode)).Cast<OperationMode>().ToList();

    [ObservableProperty]
    private OperationMode _selectedOperationMode;

    [ObservableProperty]
    private string _taskSource = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    [ObservableProperty]
    private string _taskTarget;

    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    [ObservableProperty]
    public ObservableCollection<FileExplorerTable> _taskList;

    [ObservableProperty]
    public AdvancedCollectionView _taskListACV;

    [RelayCommand]
    private async Task OnAddTaskClick(object sender)
    {
        var dialog = new AddTaskContentDialog
        {
            ViewModel = this,
            Title = "添加任务",
            PrimaryButtonText = "保存",
            CloseButtonText = "取消"
        };
        dialog.PrimaryButtonClick += OnAddTaskPrimaryButton;

        await dialog.ShowAsync();
    }

    private async void OnAddTaskPrimaryButton(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var dialog = sender as AddTaskContentDialog;
        await using var db = new AppDbContext();
        await db.FileExplorer.AddAsync(new FileExplorerTable
        {
            TaskName = dialog.TaskName,
            TaskRule = dialog.TaskRule,
            TaskSource = string.IsNullOrEmpty(TaskSource) ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : TaskSource,
            Shortcut = dialog.Shortcut,
            TaskTarget = TaskTarget,
            OperationMode = SelectedOperationMode,
            IsEnabled = true
        });
        await db.SaveChangesAsync();
    }

    [RelayCommand]
    private async Task OnSelectSourcePath()
    {
        try
        {
            var folder = await FileAndFolderPickerHelper.PickSingleFolderAsync(App.MainWindow);
            TaskSource = folder?.Path ?? "";

        }
        catch (Exception ex)
        {
            TaskSource = "";
            Logger.Error($"ServerViewModel: OnSelectMediaPath 异常信息 {ex}");
        }
    }

    [RelayCommand]
    private async Task OnSelectTargetPath()
    {
        try
        {
            var folder = await FileAndFolderPickerHelper.PickSingleFolderAsync(App.MainWindow);
            TaskTarget = folder?.Path ?? "";

        }
        catch (Exception ex)
        {
            TaskTarget = "";
            Logger.Error($"ServerViewModel: OnSelectMediaPath 异常信息 {ex}");
        }
    }

    [RelayCommand]
    private async Task OnPageLoaded()
    {
        IsActive = true;

        try
        {
            await Task.Run(() =>
            {
                dispatcherQueue.TryEnqueue(async () =>
                {
                    await using var db = new AppDbContext();
                    var list = await db.FileExplorer.ToListAsync();
                    foreach (var item in list)
                    {
                        if (item.TaskSource == Environment.GetFolderPath(Environment.SpecialFolder.Desktop))
                        {
                            item.TaskSource = "桌面";
                        }
                    }
                    TaskList = new (await db.FileExplorer.ToListAsync());
                    TaskListACV = new AdvancedCollectionView(TaskList, true);
                    TaskListACV.SortDescriptions.Add(new SortDescription("ID", SortDirection.Ascending));
                });
            });
            
        }catch(Exception ex)
        {
            IsActive = false;
            Logger.Error($"ServerViewModel: OnPageLoad 异常信息 {ex}");
        }

        IsActive = false;
    }

    [RelayCommand]
    private async Task OnUpdateTask(object dataContext)
    {
        IsActive = true;
        try
        {
            if (dataContext != null)
            {
                var dialog = new AddTaskContentDialog
                {
                    ViewModel = this,
                    Title = "修改",
                    PrimaryButtonText = "保存",
                    CloseButtonText = "取消"
                };

                var task = dataContext as FileExplorerTable;
                dialog.TaskName = task.TaskName;
                dialog.TaskRule = task.TaskRule;
                dialog.TaskSource = task.TaskSource;
                dialog.TaskTarget = task.TaskTarget;
                dialog.Shortcut = task.Shortcut;
                SelectedOperationMode = task.OperationMode;

                dialog.PrimaryButtonClick += async (s, e) =>
                {
                    await using var db = new AppDbContext();
                    var oldTask = await db.FileExplorer.Where(x => x.ID == task.ID).FirstOrDefaultAsync();
                    oldTask.TaskName = dialog.TaskName;
                    oldTask.TaskRule = dialog.TaskRule;
                    oldTask.TaskSource = TaskSource;
                    oldTask.Shortcut = dialog.Shortcut;
                    oldTask.TaskTarget = TaskTarget;
                    oldTask.OperationMode = SelectedOperationMode;
                    oldTask.IsEnabled = true;
                    await db.SaveChangesAsync();
                    await OnPageLoaded();
                    Growl.Success(new GrowlInfo
                    {
                        Message = "修改成功",
                        ShowDateTime = false
                    });
                };

                await dialog.ShowAsync();

            }

        }
        catch (Exception ex)
        {
            Growl.Error(new GrowlInfo
            {
                Message = "修改失败",
                ShowDateTime = false
            });
            Logger.Error($"ServerViewModel: OnUpdateTask 异常信息 {ex}");
            IsActive = false;
        }
        IsActive = false;
    }

    [RelayCommand]
    private async Task OnDeleteTask(object dataContext)
    {
        try
        {
            if (dataContext != null)
            {
                var task = dataContext as FileExplorerTable;
                await using var db = new AppDbContext();
                var delete = await db.FileExplorer.Where(x => x.ID == task.ID).FirstOrDefaultAsync();
                if (delete != null)
                {
                    db.FileExplorer.Remove(delete);
                }
                await db.SaveChangesAsync();
                await OnPageLoaded();
                Growl.Success(new GrowlInfo
                {
                    Message = "删除成功",
                    ShowDateTime = false
                });
            }
        }
        catch(Exception ex)
        {
            Growl.Error(new GrowlInfo
            {
                Message = "删除失败",
                ShowDateTime = false
            });
            Logger.Error($"ServerViewModel: OnDeleteTask 异常信息 {ex}");
        }
    }

    [RelayCommand]
    private void OnExecuteTask()
    {

    }

    [RelayCommand]
    private async Task OnIsEnableTask(object dataContext)
    {
        IsActive = true;
        try
        {
            if (dataContext != null)
            {
                var task = dataContext as FileExplorerTable;
                await using var db = new AppDbContext();
                var update = await db.FileExplorer.Where(x => x.ID == task.ID).FirstOrDefaultAsync();
                update.IsEnabled = !update.IsEnabled;
                await db.SaveChangesAsync();
                await OnPageLoaded();
                Growl.Success(new GrowlInfo
                {
                    Message = "修改成功",
                    ShowDateTime = false
                });
            }
        }
        catch (Exception ex)
        {
            Growl.Error(new GrowlInfo
            {
                Message = "修改失败",
                ShowDateTime = false
            });
            Logger.Error($"ServerViewModel: OnIsEnableTask 异常信息 {ex}");
            IsActive = false;
        }

    }
}

