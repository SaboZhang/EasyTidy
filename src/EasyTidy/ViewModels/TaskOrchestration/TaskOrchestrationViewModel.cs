using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using EasyTidy.Common.Database;
using EasyTidy.Model;
using EasyTidy.Util;
using EasyTidy.Views.ContentDialogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;

namespace EasyTidy.ViewModels;

public partial class TaskOrchestrationViewModel : ObservableRecipient
{
    private readonly AppDbContext _dbContext;
    public IThemeService themeService;

    public TaskOrchestrationViewModel(IThemeService themeService)
    {
        this.themeService = themeService;
        _dbContext = App.GetService<AppDbContext>();
        LoadRulesMenu();
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
    private List<string> _groupList = [];

    [ObservableProperty]
    private List<string> _groupNameList = [];

    [ObservableProperty]
    private AdvancedCollectionView _taskListACV;

    [ObservableProperty]
    private string _selectedGroupName = string.Empty;

    [ObservableProperty]
    private string _selectedTaskGroupName = string.Empty;

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

    public ObservableCollection<MenuCategory> MenuCategories { get; set; }

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
            if (dialog.HasErrors)
            {
                args.Cancel = true;
                return;
            }
            await _dbContext.TaskOrchestration.AddAsync(new TaskOrchestrationTable
            {
                TaskName = dialog.TaskName,
                TaskRule = dialog.TaskRule,
                TaskSource = SelectedOperationMode == OperationMode.Delete || SelectedOperationMode == OperationMode.RecycleBin
                ? string.Empty : TaskSource.Equals("DesktopText".GetLocalized())
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : TaskSource,
                Shortcut = dialog.Shortcut,
                TaskTarget = TaskTarget,
                OperationMode = SelectedOperationMode,
                IsEnabled = dialog.EnabledFlag,
                RuleType = dialog.RuleType,
                GroupName = !string.IsNullOrEmpty(SelectedTaskGroupName) && SelectedTaskGroupName != "AllText".GetLocalized()
                ? await GetOrUpdateTaskGroupAsync(SelectedTaskGroupName)
                : new TaskGroupTable
                {
                    GroupName = GroupTextName,
                    IsUsed = false
                },
                Filter = SelectedFilter != null 
                ? await _dbContext.Filters.Where(x => x.Id == SelectedFilter.Id).FirstOrDefaultAsync() : null,
                IsRegex = dialog.IsRegex
            });
            await _dbContext.SaveChangesAsync();
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
    /// 获取或更新任务组
    /// </summary>
    /// <param name="groupName"></param>
    /// <returns></returns>
    private async Task<TaskGroupTable> GetOrUpdateTaskGroupAsync(string groupName)
    {
        var taskGroup = await _dbContext.TaskGroup
            .FirstOrDefaultAsync(x => x.GroupName == groupName);

        if (taskGroup != null && taskGroup.IsUsed)
        {
            taskGroup.IsUsed = false;
            await _dbContext.SaveChangesAsync();
        }

        return taskGroup;
    }

    /// <summary>
    /// 选择源文件
    /// </summary>
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
                    var list = await _dbContext.TaskOrchestration.Include(x => x.GroupName).ToListAsync();
                    var filterList = await _dbContext.Filters.ToListAsync();
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
            Logger.Error($"TaskOrchestrationViewModel: OnPageLoad 异常信息 {ex}");
        }

        IsActive = false;
    }

    /// <summary>
    /// 更新
    /// </summary>
    /// <param name="dataContext"></param>
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
                SelectedTaskGroupName = task.GroupName.GroupName;
                GroupTextName = task.GroupName.GroupName;
                SelectedFilter = task.Filter;
                dialog.EnabledFlag = task.IsEnabled;
                dialog.IsRegex = task.IsRegex;
                dialog.RuleType = task.RuleType;

                dialog.PrimaryButtonClick += async (s, e) =>
                {
                    if (dialog.HasErrors)
                    {
                        e.Cancel = true;
                        return;
                    }
                    var oldTask = await _dbContext.TaskOrchestration.Include(x => x.GroupName).Where(x => x.ID == task.ID).FirstOrDefaultAsync();
                    oldTask.TaskName = dialog.TaskName;
                    oldTask.TaskRule = dialog.TaskRule;
                    oldTask.TaskSource = TaskSource;
                    oldTask.Shortcut = dialog.Shortcut;
                    oldTask.TaskTarget = TaskTarget;
                    oldTask.OperationMode = SelectedOperationMode;
                    oldTask.IsEnabled = dialog.EnabledFlag;
                    oldTask.GroupName.GroupName = GroupTextName;
                    oldTask.IsRegex = dialog.IsRegex;
                    oldTask.Filter = await _dbContext.Filters.Where(x => x.Id == SelectedFilter.Id).FirstOrDefaultAsync();
                    await _dbContext.SaveChangesAsync();
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
            Logger.Error($"TaskOrchestrationViewModel: OnUpdateTask 异常信息 {ex}");
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
                var delete = await _dbContext.TaskOrchestration.Where(x => x.ID == task.ID).FirstOrDefaultAsync();
                if (delete != null)
                {
                    _dbContext.TaskOrchestration.Remove(delete);
                }
                await _dbContext.SaveChangesAsync();
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
            Logger.Error($"TaskOrchestrationViewModel: OnDeleteTask 异常信息 {ex}");
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
                var list = await _dbContext.TaskOrchestration.Include(x => x.GroupName).ToListAsync();
            }
        }
        catch (Exception ex)
        {
            Growl.Error(new GrowlInfo
            {
                Message = "ExecutionFailedText".GetLocalized(),
                ShowDateTime = false
            });
            Logger.Error($"TaskOrchestrationViewModel: OnExecuteTask 异常信息 {ex}");
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
                var update = await _dbContext.TaskOrchestration.Where(x => x.ID == task.ID).FirstOrDefaultAsync();
                update.IsEnabled = !update.IsEnabled;
                await _dbContext.SaveChangesAsync();
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
                var query = SelectedGroupName == "AllText".GetLocalized()
                ? _dbContext.TaskOrchestration.Include(x => x.GroupName)
                : _dbContext.TaskOrchestration.Include(x => x.GroupName).Where(x => x.GroupName.GroupName == SelectedGroupName);

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
        if (string.IsNullOrEmpty(SelectedTaskGroupName) 
            || "AllText".GetLocalized().Equals(SelectedTaskGroupName))
        {
            SelectedTaskGroupName = string.Empty;
        }
        else
        {
            GroupTextName = SelectedTaskGroupName;
        }
    }

    private void LoadRulesMenu()
    {
        var orderedFloderRules = FloderRulesMenuDescription.ToList(); // 转为 List 保留顺序
        var orderedFileRules = FileRulesMenuDescription.ToList();
        MenuCategories =
        [
            new() {
                Title = "HandlingRulesForFiles".GetLocalized(),
                Items = orderedFileRules.Select(kv => kv.Value + " = " + kv.Key.GetLocalized()).ToList()
            },
            new() {
                Title = "HandlingFolderRules".GetLocalized(),
                Items = orderedFloderRules.Select(kv => kv.Value + " = " + kv.Key.GetLocalized()).ToList()
            }
        ];
    }

    private readonly Dictionary<string, string> FloderRulesMenuDescription = new Dictionary<string, string>
    {
        { "AllFoldersText","**" },
        { "AllFoldersWithoutOtherMatches","##" },
        { "AllFoldersStartingWithrobot","robot**" },
        { "AllFoldersContainingrobot","**robot**" },
        { "AllFoldersNamed_robot_LocatedUnder","C:\\**\\robot" },
        { "Use_Or_ToSeparate","image**|photo**" },
        { "UseToExcludeFoldersStartingWithmy","**/my**" },
    };

    private readonly Dictionary<string, string> FileRulesMenuDescription = new Dictionary<string, string>
    {
        { "AllFiles","*" },
        { "AllFilesWithNoOtherMatches","#" },
        { "AllFilesWithTheExtension","*.jpg" },
        { "AllFilesBeginningWith","penguin*" },
        { "AllFilesContains","*penguin*" },
        { "All_FilesUnder","C:\\Folder\\*.jpg" },
        { "UseDelimiter","*.jpg;*.png" },
        { "UseDelimitersToExclude","*.jpg/sea*" },
        { "CompressedFile","*.7z;*.bz2;*.gz;*.iso;*.rar;*.xz;*.z*.zip" },
        { "Document","*.djvu;*.doc;*.docx;*.epub;*.odt;*.pdf;*.rtp;*.tex;*.txt" },
        { "Photograph","*.bmp;*.gif;*.ico;*.jpg;*.jpeg;*.png;*.psd;*.tif;*.tiff" },
        { "Music","*.aac;*.flac;*.m4a;*mp3*.ogg;*.wma;*.wav" },
        { "PresentationAndWorksheet","*.csv;*.odp;*.ods;*.pps;*.ppt;*.pptx;*.xls;*.xlsx" },
        { "Video","*.avi;*.fly;*.m4v;*.mkv;*.mov;*.mp4;*.mpeg;*.mpg;*.wmv" },
    };


}

