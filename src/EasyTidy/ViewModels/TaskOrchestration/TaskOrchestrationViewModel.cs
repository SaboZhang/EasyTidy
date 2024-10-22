using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using EasyTidy.Model;
using EasyTidy.Util;
using EasyTidy.Views.ContentDialogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;

namespace EasyTidy.ViewModels;

public partial class TaskOrchestrationViewModel : ObservableRecipient
{
    public IThemeService themeService;

    public TaskOrchestrationViewModel(IThemeService themeService)
    {
        this.themeService = themeService;
    }

    [ObservableProperty]
    private IList<OperationMode> operationModes = Enum.GetValues(typeof(OperationMode)).Cast<OperationMode>().ToList();

    [ObservableProperty]
    private OperationMode _selectedOperationMode;

    [ObservableProperty]
    private string _taskSource = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    [ObservableProperty]
    private string _taskTarget = string.Empty;

    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    [ObservableProperty]
    private ObservableCollection<TaskOrchestrationTable> _taskList;

    [ObservableProperty]
    private List<string> _groupList = new();

    [ObservableProperty]
    private List<string> _groupNameList = new();

    [ObservableProperty]
    private AdvancedCollectionView _taskListACV;

    [ObservableProperty]
    private string _selectedGroupName = string.Empty;

    [ObservableProperty]
    private FilterTable _selectedFilter;

    [ObservableProperty]
    private ObservableCollection<FilterTable> _filterList;

    [ObservableProperty]
    private AdvancedCollectionView _filterListACV;

    [ObservableProperty]
    private int _selectedGroupIndex = -1;

    [ObservableProperty]
    private string _groupTextName = string.Empty;


    /// <summary>
    /// 添加
    /// </summary>
    /// <param name="sender"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnAddTaskClick(object sender)
    {
        var dialog = new AddTaskContentDialog
        {
            ViewModel = this,
            Title = "AdditionalText".GetLocalized(),
            PrimaryButtonText = "SaveText".GetLocalized(),
            CloseButtonText = "CancelText".GetLocalized(),
            TaskTarget = string.Empty
        };
        SelectedGroupName = string.Empty;
        SelectedGroupIndex = -1;
        dialog.PrimaryButtonClick += OnAddTaskPrimaryButton;

        await dialog.ShowAsync();
    }

    private async void OnAddTaskPrimaryButton(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            var dialog = sender as AddTaskContentDialog;
            await using var db = new AppDbContext();
            if (dialog.HasErrors)
            {
                args.Cancel = true;
                return;
            }
            await db.FileExplorer.AddAsync(new TaskOrchestrationTable
            {
                TaskName = dialog.TaskName,
                TaskRule = dialog.TaskRule,
                TaskSource = SelectedOperationMode == OperationMode.Delete || SelectedOperationMode == OperationMode.RecycleBin
                ? string.Empty : TaskSource,
                Shortcut = dialog.Shortcut,
                TaskTarget = TaskTarget,
                OperationMode = SelectedOperationMode,
                IsEnabled = dialog.EnabledFlag,
                GroupName = !string.IsNullOrEmpty(SelectedGroupName)
                ? await db.TaskGroup.Where(x => x.GroupName == SelectedGroupName).FirstOrDefaultAsync()
                : new TaskGroupTable
                {
                    GroupName = GroupTextName
                },
                Filter = SelectedFilter != null 
                ? await db.Filters.Where(x => x.Id == SelectedFilter.Id).FirstOrDefaultAsync() : null
            });
            await db.SaveChangesAsync();
            if (dialog.Shortcut)
            {
                var result = ShortcutUtil.CreateShortcutDesktop(dialog.TaskName, TaskTarget);
                if (!result)
                {
                    Growl.Error(new GrowlInfo
                    {
                        Message = "CreateShortcutFailedText".GetLocalized(),
                        ShowDateTime = false
                    });
                    Logger.Error($"TaskOrchestrationViewModel: OnAddTaskClick 创建桌面快捷方式失败");
                }
            }
            await OnPageLoaded();
            TaskTarget = string.Empty;
            SelectedFilter = null;
            SelectedGroupName = null;
            SelectedGroupIndex = -1;
            GroupTextName = string.Empty;
            Growl.Success(new GrowlInfo
            {
                Message = "SaveSuccessfulText".GetLocalized(),
                ShowDateTime = false
            });
        }
        catch (Exception ex)
        {
            Growl.Error(new GrowlInfo
            {
                Message = "SaveFailedText".GetLocalized(),
                ShowDateTime = false
            });
            Logger.Error($"TaskOrchestrationViewModel: OnAddTaskClick 异常信息 {ex}");
        }

    }

    /// <summary>
    /// 选择源文件
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnSelectSourcePath()
    {
        try
        {
            var folder = await FileAndFolderPickerHelper.PickSingleFolderAsync(App.MainWindow);
            TaskSource = folder?.Path ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        }
        catch (Exception ex)
        {
            TaskSource = "";
            Logger.Error($"TaskOrchestrationViewModel: OnSelectSourcePath 异常信息 {ex}");
        }
    }


    /// <summary>
    /// 选择目标文件
    /// </summary>
    /// <returns></returns>
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
            Logger.Error($"TaskOrchestrationViewModel: OnSelectTargetPath 异常信息 {ex}");
        }
    }

    /// <summary>
    /// 页面加载
    /// </summary>
    /// <returns></returns>
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
                    var list = await db.FileExplorer.Include(x => x.GroupName).ToListAsync();
                    var filterList = await db.Filters.ToListAsync();
                    foreach (var item in list)
                    {
                        if (item.TaskSource == Environment.GetFolderPath(Environment.SpecialFolder.Desktop))
                        {
                            item.TaskSource = "DesktopText".GetLocalized();
                        }
                    }
                    GroupList = new(list.Select(x => x.GroupName.GroupName).Distinct().ToList());
                    var newList = list.Select(x => x.GroupName.GroupName).Distinct().ToList();
                    newList.Insert(0, "AllText".GetLocalized());
                    GroupNameList = new(newList);
                    FilterList = new(filterList);
                    FilterListACV = new AdvancedCollectionView(FilterList, true);
                    FilterListACV.SortDescriptions.Add(new SortDescription("Id", SortDirection.Ascending));
                    TaskList = new(list);
                    TaskListACV = new AdvancedCollectionView(TaskList, true);
                    TaskListACV.SortDescriptions.Add(new SortDescription("ID", SortDirection.Ascending));
                });
            });

        }
        catch (Exception ex)
        {
            IsActive = false;
            Logger.Error($"FileExplorerViewModel: OnPageLoad 异常信息 {ex}");
        }

        IsActive = false;
    }

    /// <summary>
    /// 更新
    /// </summary>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnUpdateTask(object dataContext)
    {
        try
        {
            if (dataContext != null)
            {
                var dialog = new AddTaskContentDialog
                {
                    ViewModel = this,
                    Title = "ModifyText".GetLocalized(),
                    PrimaryButtonText = "SaveText".GetLocalized(),
                    CloseButtonText = "CancelText".GetLocalized(),
                };

                var task = dataContext as TaskOrchestrationTable;
                dialog.TaskName = task.TaskName;
                dialog.TaskRule = task.TaskRule;
                dialog.TaskSource = task.TaskSource;
                TaskTarget = task.TaskTarget;
                dialog.Shortcut = task.Shortcut;
                SelectedOperationMode = task.OperationMode;
                SelectedGroupName = task.GroupName.GroupName;
                GroupTextName = task.GroupName.GroupName;

                dialog.PrimaryButtonClick += async (s, e) =>
                {
                    if (dialog.HasErrors)
                    {
                        e.Cancel = true;
                        return;
                    }
                    await using var db = new AppDbContext();
                    var oldTask = await db.FileExplorer.Include(x => x.GroupName).Where(x => x.ID == task.ID).FirstOrDefaultAsync();
                    oldTask.TaskName = dialog.TaskName;
                    oldTask.TaskRule = dialog.TaskRule;
                    oldTask.TaskSource = TaskSource;
                    oldTask.Shortcut = dialog.Shortcut;
                    oldTask.TaskTarget = TaskTarget;
                    oldTask.OperationMode = SelectedOperationMode;
                    oldTask.IsEnabled = dialog.EnabledFlag;
                    oldTask.GroupName.GroupName = GroupTextName;
                    oldTask.Filter = await db.Filters.Where(x => x.Id == SelectedFilter.Id).FirstOrDefaultAsync();
                    await db.SaveChangesAsync();
                    await OnPageLoaded();
                    Growl.Success(new GrowlInfo
                    {
                        Message = "ModifySuccessfullyText".GetLocalized(),
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
                Message = "ModificationFailedText".GetLocalized(),
                ShowDateTime = false
            });
            Logger.Error($"FileExplorerViewModel: OnUpdateTask 异常信息 {ex}");
        }

    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnDeleteTask(object dataContext)
    {
        IsActive = true;
        try
        {
            if (dataContext != null)
            {
                var task = dataContext as TaskOrchestrationTable;
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
                    Message = "DeleteSuccessfulText".GetLocalized(),
                    ShowDateTime = false
                });
            }
        }
        catch (Exception ex)
        {
            Growl.Error(new GrowlInfo
            {
                Message = "DeleteFailedText".GetLocalized(),
                ShowDateTime = false
            });
            Logger.Error($"FileExplorerViewModel: OnDeleteTask 异常信息 {ex}");
            IsActive = false;
        }
        IsActive = false;
    }

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnExecuteTask(object dataContext)
    {
        IsActive = true;
        try
        {
            if (dataContext != null)
            {
                var task = dataContext as TaskOrchestrationTable;
                await using var db = new AppDbContext();

            }
        }
        catch (Exception ex)
        {
            Growl.Error(new GrowlInfo
            {
                Message = "ExecutionFailedText".GetLocalized(),
                ShowDateTime = false
            });
            Logger.Error($"FileExplorerViewModel: OnExecuteTask 异常信息 {ex}");
            IsActive = false;
        }
        IsActive = false;
    }

    /// <summary>
    /// 禁用
    /// </summary>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnIsEnableTask(object dataContext)
    {
        IsActive = true;
        try
        {
            if (dataContext != null)
            {
                var task = dataContext as TaskOrchestrationTable;
                await using var db = new AppDbContext();
                var update = await db.FileExplorer.Where(x => x.ID == task.ID).FirstOrDefaultAsync();
                update.IsEnabled = !update.IsEnabled;
                await db.SaveChangesAsync();
                await OnPageLoaded();
                Growl.Success(new GrowlInfo
                {
                    Message = "DisabledSuccessfullyText".GetLocalized(),
                    ShowDateTime = false
                });
            }
        }
        catch (Exception ex)
        {
            Growl.Error(new GrowlInfo
            {
                Message = "DisablingFailedText".GetLocalized(),
                ShowDateTime = false
            });
            Logger.Error($"FileExplorerViewModel: OnIsEnableTask 异常信息 {ex}");
            IsActive = false;
        }
        IsActive = false;

    }

    /// <summary>
    /// 分组
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnGroupNameChanged()
    {
        IsActive = true;
        try
        {
            var list = await Task.Run(async () =>
            {
                await using var db = new AppDbContext();
                var query = SelectedGroupName == "AllText".GetLocalized()
                ? db.FileExplorer.Include(x => x.GroupName)
                : db.FileExplorer.Include(x => x.GroupName).Where(x => x.GroupName.GroupName == SelectedGroupName);

                var resultList = await query.ToListAsync();

                foreach (var item in resultList)
                {
                    if (item.TaskSource == Environment.GetFolderPath(Environment.SpecialFolder.Desktop))
                    {
                        item.TaskSource = "DesktopText".GetLocalized();
                    }
                }

                return resultList;
            });

            TaskList = new(list);
            TaskListACV = new AdvancedCollectionView(TaskList, true);
            TaskListACV.SortDescriptions.Add(new SortDescription("ID", SortDirection.Ascending));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}");
            IsActive = false;
        }
        IsActive = false;
    }

    [RelayCommand]
    private Task OnSelectedItemChanged(object dataContext)
    {
        try
        {
            if (dataContext != null)
            {
                var listViews = dataContext as TeachingTip;
                var listView = listViews.HeroContent as ListView;
                var item = listView.SelectedItem;
                if (item is FilterTable filter)
                {
                    SelectedFilter = filter;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"TaskOrchestrationViewModel: OnSelectedItemChanged 异常信息 {ex}");
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void OnGroupSelectionChanged()
    {
        if (string.IsNullOrEmpty(SelectedGroupName) 
            || "AllText".GetLocalized().Equals(SelectedGroupName))
        {
            SelectedGroupName = string.Empty;
        }
        else
        {
            GroupTextName = SelectedGroupName;
        }
    }
}

