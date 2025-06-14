using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using CommunityToolkit.WinUI.Collections;
using EasyTidy.Common.Database;
using EasyTidy.Common.Extensions;
using EasyTidy.Common.Job;
using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.Service.AIService;
using EasyTidy.Views.ContentDialogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppNotifications.Builder;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System.Collections.ObjectModel;
using System.Data;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasyTidy.ViewModels;

public partial class TaskOrchestrationViewModel : ObservableRecipient
{
    private readonly AppDbContext _dbContext;

    private StackedNotificationsBehavior? _notificationQueue;

    [ObservableProperty]
    private IThemeSelectorService _themeSelectorService;

    [ObservableProperty]
    private IAppNotificationService _appNotificationService;

    public TaskOrchestrationViewModel(IThemeSelectorService themeSelectorService, AIServiceFactory factory)
    {
        _themeSelectorService = themeSelectorService;
        _dbContext = App.GetService<AppDbContext>();
        _appNotificationService = App.GetService<IAppNotificationService>();
        LoadRulesMenu();
        DateTimeModel = new ObservableCollection<PatternSnippetModel>();
        CounterModel = new ObservableCollection<PatternSnippetModel>();
        RandomizerModel = new ObservableCollection<PatternSnippetModel>();
        ReplaceModel = new ObservableCollection<PatternSnippetModel>();
        InitializeRenameModel();
    }

    [ObservableProperty]
    private IList<OperationMode> operationModes = Enum.GetValues(typeof(OperationMode)).Cast<OperationMode>().Where(x => x != OperationMode.None).ToList();

    [ObservableProperty]
    private IList<Encrypted> _encrypteds = Enum.GetValues(typeof(Encrypted)).Cast<Encrypted>().ToList();

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

    [ObservableProperty]
    private ObservableCollection<PatternSnippetModel> _dateTimeModel;

    [ObservableProperty]
    public ObservableCollection<PatternSnippetModel> _counterModel;

    [ObservableProperty]
    public ObservableCollection<PatternSnippetModel> _randomizerModel;

    [ObservableProperty]
    public ObservableCollection<PatternSnippetModel> _replaceModel;

    [ObservableProperty]
    private bool _isExecuting = false;

    [ObservableProperty]
    private string _isDefaultContent = "\uE734";

    private bool? _orederChecked;

    public bool OrederChecked
    {
        get => _orederChecked ?? Settings.IdOrder;
        set
        {
            if (_orederChecked != value)
            {
                _orederChecked = value;
                OnPropertyChanged();
            }
        }
    }

    [ObservableProperty]
    private bool _isTipOpen;

    [ObservableProperty]
    private string _aiNotice = "AI_Notice".GetLocalized();

    private static DateTime LastInvocationTime = DateTime.MinValue;

    public void Initialize(StackedNotificationsBehavior notificationQueue)
    {
        _notificationQueue = notificationQueue;
    }

    public void Uninitialize()
    {
        _notificationQueue = null;
    }

    /// <summary>
    /// 添加
    /// </summary>
    /// <param name="sender"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnAddTaskClick(object sender)
    {
        var dialog = new TaskContentEditorDialog
        {
            ViewModel = this,
            Title = "AdditionalText".GetLocalized(),
            PrimaryButtonText = "SaveText".GetLocalized(),
            CloseButtonText = "CancelText".GetLocalized(),
            TaskTarget = string.Empty,
            Password = string.IsNullOrEmpty(Settings.EncryptedPassword)
            ? string.Empty
            : CryptoUtil.DesDecrypt(Settings.EncryptedPassword),
            Encencrypted = Settings.Encrypted,
            IsSourceFile = Settings.OriginalFile
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
            var dialog = sender as TaskContentEditorDialog;
            var ai = await _dbContext.AIService.Where(x => x.IsDefault == true && x.IsEnabled == true).FirstOrDefaultAsync();
            if (ai == null && (SelectedOperationMode == OperationMode.AIClassification || SelectedOperationMode == OperationMode.AISummary))
            {
                var tips = new AppNotificationBuilder().AddText("AI_Notice".GetLocalized())
                    .AddButton(new AppNotificationButton("Settings".GetLocalized())
                    .AddArgument("action", "AiSettings")).AddArgument("contentId", "351");
                AppNotificationService.Show(tips.BuildNotification().Payload);
                IsTipOpen = true;
                args.Cancel = true;
                return;
            }
            if (!dialog.IsValid || dialog.HasErrors || string.IsNullOrEmpty(dialog.TaskRule) || string.IsNullOrEmpty(GroupTextName))
            {
                args.Cancel = true;
                return;
            }
            Settings.EncryptedPassword = string.IsNullOrEmpty(Settings.EncryptedPassword)
            ? CryptoUtil.DesEncrypt(dialog.Password)
            : Settings.EncryptedPassword;
            Settings.Encrypted = dialog.Encencrypted;
            Settings.OriginalFile = dialog.IsSourceFile;
            var prompt = SelectedOperationMode switch
            {
                OperationMode.AIClassification => GetUserDefinePromptsJson(string.Empty, dialog.CustomPrompt, "分类"),
                OperationMode.AISummary when dialog.SelectedMode == PromptType.Custom => GetUserDefinePromptsJson(dialog.SystemPrompt, dialog.UserPrompt, "总结"),
                _ => string.Empty
            };
            string groupNameToUse = !string.IsNullOrEmpty(SelectedTaskGroupName) ? SelectedTaskGroupName : GroupTextName;
            var group = !string.IsNullOrEmpty(groupNameToUse)
                && SelectedTaskGroupName != "AllText".GetLocalized()
                ? await GetOrUpdateTaskGroupAsync(!string.IsNullOrEmpty(SelectedTaskGroupName) ? SelectedTaskGroupName : GroupTextName)
                : new TaskGroupTable
                {
                    GroupName = GroupTextName,
                    IsUsed = false,
                    IsDefault = false
                };
            await _dbContext.TaskOrchestration.AddAsync(new TaskOrchestrationTable
            {
                TaskName = dialog.TaskName,
                TaskRule = string.IsNullOrEmpty(dialog.TaskRule) && SelectedOperationMode == OperationMode.AIClassification ? "*" : dialog.TaskRule,
                TaskSource = SelectedOperationMode == OperationMode.Delete || SelectedOperationMode == OperationMode.RecycleBin
                ? TaskTarget : ExtractBasePath(TaskTarget, SelectedOperationMode, TaskSource),
                Shortcut = dialog.Shortcut,
                TaskTarget = TaskTarget,
                OperationMode = SelectedOperationMode,
                IsEnabled = dialog.EnabledFlag,
                RuleType = dialog.RuleType,
                CreateTime = DateTime.Now,
                Priority = 1,
                GroupName = group,
                Filter = SelectedFilter != null
                ? await _dbContext.Filters.Where(x => x.Id == SelectedFilter.Id).FirstOrDefaultAsync() : null,
                IsRegex = dialog.IsRegex,
                AIIdentify = ai != null
                && (SelectedOperationMode == OperationMode.AIClassification
                || SelectedOperationMode == OperationMode.AISummary) ? ai.Identify : Guid.Empty,
                UserDefinePromptsJson = prompt,
                Argument = dialog.Argument
            });
            await _dbContext.SaveChangesAsync();
            if (dialog.Shortcut)
            {
                var result = ShortcutUtil.CreateShortcutDesktop(dialog.TaskName, TaskTarget);
                if (!result)
                {
                    _notificationQueue.ShowWithWindowExtension("CreateShortcutFailedText".GetLocalized(), InfoBarSeverity.Error, 3000);
                    Logger.Error($"TaskOrchestrationViewModel: OnAddTaskClick {"CreateShortcutFailedText".GetLocalized()}");
                }
            }
            await OnPageLoaded();
            TaskTarget = string.Empty;
            SelectedFilter = null;
            SelectedGroupName = "AllText".GetLocalized();
            GroupTextName = string.Empty;
            _notificationQueue.ShowWithWindowExtension("SaveSuccessfulText".GetLocalized(), InfoBarSeverity.Success, 3000);
        }
        catch (Exception ex)
        {
            _notificationQueue.ShowWithWindowExtension("SaveFailedText".GetLocalized(), InfoBarSeverity.Error, 3000);
            Logger.Error($"TaskOrchestrationViewModel: OnAddTaskClick {"SaveFailedText".GetLocalized()} Exception: {ex}");
        }

    }

    /// <summary>
    /// 处理路径
    /// </summary>
    /// <param name="targetPath"></param>
    /// <param name="operation"></param>
    /// <param name="sourcePath"></param>
    /// <returns></returns>
    private string ExtractBasePath(string targetPath, OperationMode operation, string sourcePath)
    {
        // 处理重命名模式
        if (operation == OperationMode.Rename)
        {
            // 获取所有模式代码并替换
            targetPath = RemovePatternsFromPath(targetPath);
            return targetPath.TrimEnd('\\');
        }

        // 处理特殊路径 Desktop
        return IsLocalizedDesktopPath(sourcePath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : sourcePath;
    }

    // 获取所有模式代码并移除
    private string RemovePatternsFromPath(string path)
    {
        var allPatterns = GetAllPatterns();
        foreach (var pattern in allPatterns)
        {
            path = path.Replace(pattern, "");
        }
        return path;
    }

    // 获取 DateTimeModel、CounterModel 和 RandomizerModel 的所有代码模式
    private IEnumerable<string> GetAllPatterns()
    {
        return DateTimeModel.Select(x => x.Code)
            .Concat(CounterModel.Select(x => x.Code))
            .Concat(RandomizerModel.Select(x => x.Code))
            .Concat(ReplaceModel.Select(x => x.Code));
    }

    // 判断 sourcePath 是否为桌面路径
    private bool IsLocalizedDesktopPath(string path)
    {
        return path.Equals("DesktopText".GetLocalized(), StringComparison.OrdinalIgnoreCase);
    }

    private static readonly JsonSerializerOptions CachedJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static string GetUserDefinePromptsJson(string systemPrompt, string userPrompt, string promptName)
    {
        var promptConfigs = new List<object>
        {
            new
            {
                enabled = true,
                name = promptName,
                prompts = new[]
                {
                    new {
                        content = string.IsNullOrEmpty(systemPrompt) ? "" : systemPrompt,
                        role = "system"
                    },
                    new {
                        content = string.IsNullOrEmpty(userPrompt) ? "" : userPrompt,
                        role = "user"
                    }
                }
            }
        };

        return JsonSerializer.Serialize(promptConfigs, CachedJsonOptions);
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

        if (taskGroup != null)
        {
            if (taskGroup.IsUsed)
            {
                taskGroup.IsUsed = false;
                _dbContext.TaskGroup.Update(taskGroup);
            }
            return taskGroup;
        }

        var newGroup = new TaskGroupTable
        {
            GroupName = groupName,
            IsUsed = false,
            IsDefault = false
        };

        await _dbContext.TaskGroup.AddAsync(newGroup);
        await _dbContext.SaveChangesAsync();

        return newGroup;
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
            Logger.Error($"TaskOrchestrationViewModel: OnSelectSourcePath {"ExceptionTxt".GetLocalized()} {ex}");
        }
    }

    [RelayCommand]
    private async Task OnSelectRunSourcePath()
    {
        try
        {
            var runFile = await FileAndFolderPickerHelper.PickSingleFileAsync(App.MainWindow, ["*"]);
            TaskSource = runFile?.Path ?? "";
        }
        catch (Exception ex)
        {
            TaskSource = "";
            Logger.Error($"TaskOrchestrationViewModel: OnSelectRunPath {"ExceptionTxt".GetLocalized()} {ex}");
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
            Logger.Error($"TaskOrchestrationViewModel: OnSelectTargetPath {"ExceptionTxt".GetLocalized()} {ex}");
        }
    }

    /// <summary>
    /// 页面加载
    /// </summary>
    [RelayCommand]
    private async Task OnPageLoaded()
    {
        IsActive = true;
        IsDefaultContent = "\uE734";

        try
        {
            // ✅ 1. 后台线程处理数据库加载
            var list = await _dbContext.TaskOrchestration.Include(x => x.GroupName).ToListAsync();
            var filterList = await _dbContext.Filters.ToListAsync();

            // ✅ 2. 主线程处理 UI 更新
            dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var item in list)
                {
                    if (item.TaskSource == Environment.GetFolderPath(Environment.SpecialFolder.Desktop))
                    {
                        item.TaskSource = "DesktopText".GetLocalized();
                    }
                    item.TagOrder = Settings.IdOrder;
                }

                // ✅ 防止 GroupName 为 null
                var groupNames = list
                    .Where(x => x.GroupName != null)
                    .Select(x => x.GroupName.GroupName)
                    .Distinct()
                    .ToList();

                GroupList = new(groupNames);

                var newList = new List<string> { "AllText".GetLocalized() };
                newList.AddRange(groupNames);

                GroupNameList = new(newList);
                FilterList = new(filterList);

                FilterListACV = new AdvancedCollectionView(FilterList, true);
                FilterListACV.SortDescriptions.Add(new SortDescription("Id", SortDirection.Ascending));

                TaskList = new(list);
                TaskListACV = new AdvancedCollectionView(TaskList, true);
                TaskListACV.SortDescriptions.Add(new SortDescription("Priority", SortDirection.Descending));
                TaskListACV.SortDescriptions.Add(new SortDescription("ID", SortDirection.Ascending));
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"TaskOrchestrationViewModel: OnPageLoad {"ExceptionTxt".GetLocalized()} {ex}");
        }
        finally
        {
            IsActive = false;
        }
    }

    /// <summary>
    /// 更新
    /// </summary>
    /// <param name="dataContext"></param>
    [RelayCommand]
    private async Task OnUpdateTask(object dataContext)
    {
        var currentTime = DateTime.Now;
        if ((currentTime - LastInvocationTime).TotalMilliseconds < 500) // 防抖时间间隔，单位为毫秒
        {
            Logger.Info("触发防抖了，鼠标坏了吧，500毫秒两次点击🤣🤣🤣");
            return;
        }
        LastInvocationTime = currentTime;
        try
        {
            if (dataContext != null)
            {
                var dialog = new TaskContentEditorDialog
                {
                    ViewModel = this,
                    Title = "ModifyText".GetLocalized(),
                    PrimaryButtonText = "SaveText".GetLocalized(),
                    CloseButtonText = "CancelText".GetLocalized(),
                };

                var task = dataContext as TaskOrchestrationTable;
                dialog.TaskName = task.TaskName;
                dialog.TaskRule = task.TaskRule;
                TaskSource = task.TaskSource;
                TaskTarget = task.TaskTarget;
                dialog.Shortcut = task.Shortcut;
                SelectedOperationMode = task.OperationMode;
                SelectedTaskGroupName = task.GroupName.GroupName;
                GroupTextName = task.GroupName.GroupName;
                SelectedFilter = task.Filter;
                dialog.EnabledFlag = task.IsEnabled;
                dialog.IsRegex = task.IsRegex;
                dialog.RuleType = task.RuleType;
                dialog.IsSourceFile = Settings.OriginalFile;
                dialog.Password = string.IsNullOrEmpty(Settings.EncryptedPassword)
                ? string.Empty
                : CryptoUtil.DesDecrypt(Settings.EncryptedPassword);
                dialog.Encencrypted = Settings.Encrypted;
                dialog.SelectedMode = PromptType.BuiltIn;
                dialog.Argument = task.Argument;
                if (task.OperationMode == OperationMode.AIClassification)
                {
                    dialog.CustomPrompt = FilterUtil.ParseUserDefinePrompt(task.UserDefinePromptsJson);
                }
                if (task.OperationMode == OperationMode.AISummary && !string.IsNullOrEmpty(task.UserDefinePromptsJson))
                {
                    dialog.SelectedMode = PromptType.Custom;
                    dialog.SystemPrompt = FilterUtil.ParseUserDefinePrompt(task.UserDefinePromptsJson, "总结", "system");
                    dialog.UserPrompt = FilterUtil.ParseUserDefinePrompt(task.UserDefinePromptsJson, "总结", "user");
                }

                dialog.PrimaryButtonClick += async (s, e) =>
                {
                    if (dialog.HasErrors)
                    {
                        e.Cancel = true;
                        return;
                    }
                    var oldTask = await _dbContext.TaskOrchestration.Include(x => x.GroupName).Where(x => x.ID == task.ID).FirstOrDefaultAsync();
                    await UpdateQuartzGroup(oldTask.GroupName.GroupName, $"{oldTask.TaskName}#{oldTask.ID}", GroupTextName, $"{dialog.TaskName}#{oldTask.ID}");
                    oldTask.TaskName = dialog.TaskName;
                    var group = await GroupUpdateOrCreate(GroupTextName);
                    oldTask.TaskRule = dialog.TaskRule;
                    oldTask.TaskSource = TaskSource;
                    oldTask.Shortcut = dialog.Shortcut;
                    oldTask.TaskTarget = TaskTarget;
                    oldTask.OperationMode = SelectedOperationMode;
                    oldTask.IsEnabled = dialog.EnabledFlag;
                    oldTask.GroupName = group;
                    oldTask.IsRegex = dialog.IsRegex;
                    oldTask.RuleType = dialog.RuleType;
                    Settings.EncryptedPassword = dialog.Password;
                    Settings.Encrypted = dialog.Encencrypted;
                    Settings.OriginalFile = dialog.IsSourceFile;
                    oldTask.Argument = dialog.Argument;
                    oldTask.Filter = SelectedFilter != null
                    ? await _dbContext.Filters.Where(x => x.Id == SelectedFilter.Id).FirstOrDefaultAsync()
                    : null;
                    var prompt = SelectedOperationMode switch
                    {
                        OperationMode.AIClassification => GetUserDefinePromptsJson(string.Empty, dialog.CustomPrompt, "分类"),
                        OperationMode.AISummary when dialog.SelectedMode == PromptType.Custom => GetUserDefinePromptsJson(dialog.SystemPrompt, dialog.UserPrompt, "总结"),
                        _ => string.Empty
                    };
                    oldTask.UserDefinePromptsJson = prompt;
                    await _dbContext.SaveChangesAsync();
                    await OnPageLoaded();
                    _notificationQueue.ShowWithWindowExtension("ModifySuccessfullyText".GetLocalized(), InfoBarSeverity.Success, 3000);
                };

                await dialog.ShowAsync();

            }

        }
        catch (Exception ex)
        {
            _notificationQueue.ShowWithWindowExtension("ModificationFailedText".GetLocalized(), InfoBarSeverity.Error, 3000);
            Logger.Error($"TaskOrchestrationViewModel: OnUpdateTask {"ExceptionTxt".GetLocalized()} {ex}");
        }

    }

    private async Task<TaskGroupTable> GroupUpdateOrCreate(string groupTextName)
    {
        try
        {
            var group = await _dbContext.TaskGroup.FirstOrDefaultAsync(g => g.GroupName == groupTextName);
            if (group == null)
            {
                group = new TaskGroupTable()
                {
                    GroupName = GroupTextName
                };
                await _dbContext.TaskGroup.AddAsync(group);
                await _dbContext.SaveChangesAsync();
            }
            return group;
        }
        catch (Exception ex)
        {
            Logger.Error($"TaskOrchestrationViewModel: GroupUpdateOrCreate {"ExceptionTxt".GetLocalized()} {ex}");
            return null;
        }
    }

    private async Task UpdateQuartzGroup(string groupName, string taskName, string newGroupName, string newTaskName)
    {
        try
        {
            await QuartzHelper.UpdateJob(taskName, groupName, newTaskName, newGroupName);
        }
        catch (Exception ex)
        {
            Logger.Error($"TaskOrchestrationViewModel: UpdateQuartzGroup {"ExceptionTxt".GetLocalized()} {ex}");
        }

    }

    [RelayCommand]
    private async Task OnImportTask()
    {
        try
        {
            var file = await FileAndFolderPickerHelper.PickSingleFileAsync(App.MainWindow, [".xls", ".xlsx"]);
            if (file == null || string.IsNullOrEmpty(file.Path))
            {
                _notificationQueue.ShowWithWindowExtension("FileNotSelectedText".GetLocalized(), InfoBarSeverity.Warning);
                return;
            }
            var tasks = ExcelImportHelper.ImportFromExcel(file.Path, 2, row =>
            {
                try
                {
                    return new OrchestrationTask
                    {
                        GroupName = row.GetCellValue(0),
                        TaskName = row.GetCellValue(1),
                        Rule = row.GetCellValue(2),
                        IsRegex = row.GetCellValue(3),
                        Action = row.GetCellValue(4),
                        SourcePath = row.GetCellValue(5),
                        TargetPath = row.GetCellValue(6),
                    };
                }
                catch (Exception ex)
                {
                    Logger.Error($"TaskOrchestrationViewModel: OnImportTask {"ExceptionTxt".GetLocalized()} {ex}");
                    return null;
                }
            });
            var importName = Path.GetFileNameWithoutExtension(file.Path);
            await SaveImportedTasksAsync(tasks, importName);
            await OnPageLoaded();
            _notificationQueue.ShowWithWindowExtension("ImportSuccessfullyText".GetLocalized(), InfoBarSeverity.Success, 3000);
        }
        catch (Exception ex)
        {
            if (ex is ArgumentException)
            {
                _notificationQueue.ShowWithWindowExtension(ex.Message, InfoBarSeverity.Error, 3000);
            }
            if (ex is IOException)
            {
                _notificationQueue.ShowWithWindowExtension("ExclusivePermissionException".GetLocalized(), InfoBarSeverity.Error, 3000);
            }
            Logger.Error($"TaskOrchestrationViewModel: OnImportTask {"ExceptionTxt".GetLocalized()} {ex}");
        }
    }

    private async Task SaveImportedTasksAsync(List<OrchestrationTask> tasks, string importName)
    {
        var defaultGroupName = $"{importName}{DateTime.Now:yyyy-MM-dd}";
        // 1. 收集所有不为空的组名
        var groupNames = tasks
            .Select(t => string.IsNullOrEmpty(t.GroupName) ? defaultGroupName : t.GroupName)
            .Distinct()
            .ToList();

        // 2. 一次性查出已有组
        var existingGroups = await _dbContext.TaskGroup
            .Where(g => groupNames.Contains(g.GroupName))
            .ToListAsync();

        // 3. 创建字典加快查找
        var groupDict = existingGroups.ToDictionary(g => g.GroupName, g => g);

        // 4. 逐个任务处理
        var sb = new StringBuilder();
        foreach (var task in tasks)
        {
            var groupName = string.IsNullOrEmpty(task.GroupName) ? defaultGroupName : task.GroupName;

            if (!groupDict.TryGetValue(groupName, out var group))
            {
                group = new TaskGroupTable
                {
                    GroupName = groupName,
                    IsUsed = false,
                    IsDefault = false
                };
                await _dbContext.TaskGroup.AddAsync(group);
                groupDict[groupName] = group; // 添加到缓存中，防止后续重复创建
            }

            var entity = task.ToEntity(group);
            if (entity.OperationMode == OperationMode.None)
            {
                sb.Append(entity.TaskName + ", ");
            }
            await _dbContext.TaskOrchestration.AddAsync(entity);
        }
        if (sb.Length > 0)
        {
            sb.Length -= 2; // 移除最后的逗号和空格
            _notificationQueue.ShowWithWindowExtension(I18n.Format("TaskCannotBeRecognized", sb.ToString()), InfoBarSeverity.Warning, 5000);
        }
        await _dbContext.SaveChangesAsync();
    }

    [RelayCommand]
    private async Task OnSaveExcelTemplateAsync()
    {
        try
        {
            var fileTypeChoices = new Dictionary<string, IList<string>>
            {
                { "Excel", new List<string> { ".xlsx", ".xls" } }
            };
            var suggestedFilename = I18n.Format("SaveTemplate", Constants.AppName, Constants.Version);
            var file = await FileAndFolderPickerHelper.PickSaveFileAsync(App.MainWindow, fileTypeChoices, suggestedFilename);
            if (file != null)
            {
                using var stream = await file.OpenStreamForWriteAsync();
                IWorkbook workbook = new XSSFWorkbook();
                var sheet = workbook.CreateSheet("ImportTemplate".GetLocalized());

                ExcelImportHelper.CreateTemplate(workbook, sheet);

                // 写入文件
                workbook.Write(stream);
                _notificationQueue.ShowWithWindowExtension(I18n.Format("SaveTemplateSuccessText", file.Path), InfoBarSeverity.Success, 3000);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"TaskOrchestrationViewModel: OnSaveExcelTemplateAsync {"ExceptionTxt".GetLocalized()} {ex}");
            _notificationQueue.ShowWithWindowExtension("SaveTemplateFailedText".GetLocalized(), InfoBarSeverity.Error, 3000);
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
            if (dataContext is not TaskOrchestrationTable task)
            {
                _notificationQueue.ShowWithWindowExtension("InvalidTaskData".GetLocalized(), InfoBarSeverity.Error);
                return;
            }

            var delete = await _dbContext.TaskOrchestration
                .Include(x => x.GroupName)
                .Include(x => x.AutomaticTable)
                .FirstOrDefaultAsync(x => x.ID == task.ID);

            if (delete == null)
            {
                _notificationQueue.ShowWithWindowExtension("TaskNotFoundText".GetLocalized(), InfoBarSeverity.Warning);
                return;
            }

            var group = delete.GroupName;

            await DeleteAssociatedAutomaticTable(delete.AutomaticTable, delete.ID);
            _dbContext.TaskOrchestration.Remove(delete);
            await _dbContext.SaveChangesAsync();
            await DeleteEmptyGroup(group);

            // 删除Quartz任务和停止文件监控
            await DeleteQuartzJob(task);
            FileEventHandler.StopMonitor(task.TaskSource, task.TaskTarget);

            // 刷新页面并通知用户
            TaskListACV.Remove(task);
            TaskListACV.Refresh();
            _notificationQueue.ShowWithWindowExtension("DeleteSuccessfulText".GetLocalized(), InfoBarSeverity.Success, 3000);
        }
        catch (Exception ex)
        {
            Logger.Error($"TaskOrchestrationViewModel: OnDeleteTask {"ExceptionTxt".GetLocalized()} {ex}");
            _notificationQueue.ShowWithWindowExtension("DeleteFailedText".GetLocalized(), InfoBarSeverity.Error, 3000);
        }
        finally
        {
            IsActive = false;
        }
    }

    private async Task DeleteAssociatedAutomaticTable(AutomaticTable automaticTable, int taskId)
    {
        if (automaticTable == null) return;

        var scheduleId = automaticTable?.Schedule?.ID;
        var schedule = await _dbContext.Schedule
            .FirstOrDefaultAsync(x => x.ID == scheduleId);

        if (schedule != null)
        {
            _dbContext.Schedule.Remove(schedule);
        }

        var isAutomaticTableInUse = await _dbContext.TaskOrchestration
            .AnyAsync(x => x.AutomaticTable.ID == automaticTable.ID && x.ID != taskId);

        if (!isAutomaticTableInUse)
        {
            _dbContext.Automatic.Remove(automaticTable);
        }
    }

    private async Task DeleteEmptyGroup(TaskGroupTable group)
    {
        if (group == null) return;

        var hasOtherTasks = await _dbContext.TaskOrchestration
            .AnyAsync(x => x.GroupName.Id == group.Id);

        if (!hasOtherTasks)
        {
            GroupList.Remove(group.GroupName);
            GroupNameList.Remove(group.GroupName);
            _dbContext.TaskGroup.Remove(group);
            await _dbContext.SaveChangesAsync();
        }
    }

    private async Task DeleteQuartzJob(TaskOrchestrationTable task)
    {
        await Task.Delay(10);
        if (task.GroupName == null) return;
        await QuartzHelper.DeleteJob(task.TaskName + "#" + task.ID, task.GroupName.GroupName);
    }

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnExecuteTask(object dataContext)
    {
        var currentTime = DateTime.Now;
        if ((currentTime - LastInvocationTime).TotalMilliseconds < 500) // 防抖时间间隔，单位为毫秒
        {
            Logger.Info($"触发防抖了，鼠标坏了吧，500毫秒两次点击🤣🤣🤣");
            return;
        }
        LastInvocationTime = currentTime;
        IsActive = true;
        try
        {
            if (dataContext != null)
            {
                var task = dataContext as TaskOrchestrationTable;
                string language = string.IsNullOrEmpty(Settings.Language)
                ? "Please respond in the language of the provided content." : Settings.Language;
                var automatic = new AutomaticJob();
                var rule = await automatic.GetSpecialCasesRule(task.GroupName.Id, task.TaskRule);
                var ai = await _dbContext.AIService.Where(x => x.Identify.ToString().ToLower().Equals(task.AIIdentify.ToString().ToLower())).FirstOrDefaultAsync();
                IAIServiceLlm llm = null;
                if (task.OperationMode == OperationMode.AIClassification || task.OperationMode == OperationMode.AISummary)
                {
                    llm = AIServiceFactory.CreateAIServiceLlm(ai, task.UserDefinePromptsJson);
                }
                var operationParameters = new OperationParameters(
                    operationMode: task.OperationMode,
                    sourcePath: task.TaskSource.Equals("DesktopText".GetLocalized())
                    ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    : task.TaskSource,
                    targetPath: task.TaskTarget.Equals("DesktopText".GetLocalized())
                    ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    : task.TaskTarget,
                    fileOperationType: Settings.GeneralConfig.FileOperationType,
                    handleSubfolders: Settings.GeneralConfig.SubFolder ?? false,
                    funcs: FilterUtil.GeneratePathFilters(rule, task.RuleType),
                    pathFilter: FilterUtil.GetPathFilters(task.Filter),
                    ruleModel: new RuleModel { Filter = task.Filter, Rule = task.TaskRule, RuleType = task.RuleType })
                { RuleName = task.TaskRule, AIServiceLlm = llm, Prompt = task.UserDefinePromptsJson, Argument = task.Argument, Language = language };
                await OperationHandler.ExecuteOperationAsync(task.OperationMode, operationParameters);
                _notificationQueue.ShowWithWindowExtension("ExecutionSuccessfulText".GetLocalized(), InfoBarSeverity.Success, 3000);
            }
        }
        catch (Exception ex)
        {
            _notificationQueue.ShowWithWindowExtension("ExecutionFailedText".GetLocalized(), InfoBarSeverity.Error, 3000);
            Logger.Error($"TaskOrchestrationViewModel: OnExecuteTask {"ExceptionTxt".GetLocalized()} {ex}");
            IsActive = false;
        }
        IsActive = false;
    }

    /// <summary>
    /// 执行指定的分组
    /// </summary>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnExecuteGroupTask()
    {
        IsActive = true;
        try
        {
            if (IsExecuting)
            {
                var list = await _dbContext.TaskOrchestration.Include(x => x.GroupName)
                    .Where(x => x.GroupName.GroupName == SelectedGroupName).ToListAsync();
                if (list != null)
                {
                    var orderList = list.OrderByDescending(x => x.Priority);
                    string language = string.IsNullOrEmpty(Settings.Language) ? "Follow the document language" : Settings.Language;
                    foreach (var item in orderList)
                    {
                        var ai = await _dbContext.AIService.Where(x => x.Identify.ToString().ToLower().Equals(item.AIIdentify.ToString().ToLower())).FirstOrDefaultAsync();
                        IAIServiceLlm llm = null;
                        if (item.OperationMode == OperationMode.AIClassification || item.OperationMode == OperationMode.AISummary)
                        {
                            llm = AIServiceFactory.CreateAIServiceLlm(ai, item.UserDefinePromptsJson);
                        }
                        var automatic = new AutomaticJob();
                        var rule = await automatic.GetSpecialCasesRule(item.GroupName.Id, item.TaskRule);
                        var operationParameters = new OperationParameters(
                            operationMode: item.OperationMode,
                            sourcePath: item.TaskSource.Equals("DesktopText".GetLocalized())
                            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                            : item.TaskSource,
                            targetPath: item.TaskTarget.Equals("DesktopText".GetLocalized())
                            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                            : item.TaskTarget,
                            fileOperationType: Settings.GeneralConfig.FileOperationType,
                            handleSubfolders: Settings.GeneralConfig.SubFolder ?? false,
                            funcs: FilterUtil.GeneratePathFilters(rule, item.RuleType),
                            pathFilter: FilterUtil.GetPathFilters(item.Filter),
                            ruleModel: new RuleModel { Filter = item.Filter, Rule = item.TaskRule, RuleType = item.RuleType })
                        { RuleName = item.TaskRule, AIServiceLlm = llm, Prompt = item.UserDefinePromptsJson, Argument = item.Argument, Language = language };
                        await OperationHandler.ExecuteOperationAsync(item.OperationMode, operationParameters);
                        _notificationQueue.ShowWithWindowExtension("ExecutionSuccessfulText".GetLocalized(), InfoBarSeverity.Success, 3000);

                    }
                }
            }
            IsActive = false;
        }
        catch (Exception ex)
        {
            _notificationQueue.ShowWithWindowExtension("ExecutionFailedText".GetLocalized(), InfoBarSeverity.Error, 3000);
            Logger.Error($"TaskOrchestrationViewModel: OnExecuteGroupTask {"ExceptionTxt".GetLocalized()} {ex}");
            IsActive = false;
        }
    }

    /// <summary>
    /// 禁用/启用
    /// </summary>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnIsEnableTask(object dataContext)
    {
        var currentTime = DateTime.Now;
        if ((currentTime - LastInvocationTime).TotalMilliseconds < 500) // 防抖时间间隔，单位为毫秒
        {
            Logger.Info($"触发防抖了，鼠标坏了吧，500毫秒两次点击🤣🤣🤣");
            return;
        }
        LastInvocationTime = currentTime;
        IsActive = true;
        try
        {
            if (dataContext != null)
            {
                var task = dataContext as TaskOrchestrationTable;
                var update = await _dbContext.TaskOrchestration.Where(x => x.ID == task.ID).FirstOrDefaultAsync();
                update.IsEnabled = !update.IsEnabled;
                await _dbContext.SaveChangesAsync();
                await (update.IsEnabled
                    ? QuartzHelper.ResumeJob(task.TaskName + "#" + task.ID.ToString(), task.GroupName.GroupName)
                    : QuartzHelper.PauseJob(task.TaskName + "#" + task.ID.ToString(), task.GroupName.GroupName));
                await OnPageLoaded();
                _notificationQueue.ShowWithWindowExtension("DisabledSuccessfullyText".GetLocalized(), InfoBarSeverity.Success, 3000);
            }
        }
        catch (Exception ex)
        {
            _notificationQueue.ShowWithWindowExtension("DisablingFailedText".GetLocalized(), InfoBarSeverity.Error, 3000);
            Logger.Error($"FileExplorerViewModel: OnIsEnableTask {"ExceptionTxt".GetLocalized()} {ex}");
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
        IsExecuting = false;
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

            if (!string.IsNullOrEmpty(SelectedGroupName) && !SelectedGroupName.Equals("AllText".GetLocalized()) && list.Count != 0)
            {
                IsExecuting = true;
                foreach (var item in list)
                {
                    if (item.GroupName.IsDefault)
                    {
                        IsDefaultContent = "\uE735";
                    }
                    else
                    {
                        IsDefaultContent = "\uE734";
                    }
                }
            }
            else
            {
                IsDefaultContent = "\uE734";
            }

            TaskList = new(list);
            TaskListACV = new AdvancedCollectionView(TaskList, true);
            TaskListACV.SortDescriptions.Add(new SortDescription("Priority", SortDirection.Descending));
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
            Logger.Error($"TaskOrchestrationViewModel: OnSelectedItemChanged {"ExceptionTxt".GetLocalized()} {ex}");
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

    [RelayCommand]
    private async Task OnSetDefaultGroup()
    {
        if (string.IsNullOrEmpty(SelectedGroupName))
        {
            _notificationQueue.ShowWithWindowExtension("DefaultGroup_Warning".GetLocalized(), InfoBarSeverity.Warning, 3000);
            return;
        }

        var group = await _dbContext.TaskGroup.FirstOrDefaultAsync(x => x.GroupName == SelectedGroupName);
        if (group == null || string.IsNullOrEmpty(SelectedGroupName))
        {
            _notificationQueue.ShowWithWindowExtension(I18n.Format("InvalidGrouping_Warning", SelectedGroupName), InfoBarSeverity.Warning, 3000);
            return;
        }

        var defaultGroup = await _dbContext.TaskGroup.FirstOrDefaultAsync(x => x.IsDefault);
        bool hasChanges = false;

        if (defaultGroup != null && defaultGroup.Id != group.Id)
        {
            defaultGroup.IsDefault = false;
            group.IsDefault = true;
            hasChanges = true;
        }
        else if (defaultGroup == null)
        {
            group.IsDefault = true;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _dbContext.SaveChangesAsync();
        }

        await OnGroupNameChanged();
    }

    [RelayCommand]
    private async Task OnItemClickChanged(object dataContext)
    {
        try
        {
            if (dataContext != null)
            {
                var listView = dataContext as ListView;
                var item = listView.Items[0];
                if (item is FilterTable filter)
                {
                    SelectedFilter = filter;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"TaskOrchestrationViewModel: OnSelectedItemChanged {"ExceptionTxt".GetLocalized()} {ex}");
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OnCopyTaskAsync(object obj)
    {
        if (obj as TaskOrchestrationTable is TaskOrchestrationTable task)
        {
            var oldTask = await _dbContext.TaskOrchestration.FirstOrDefaultAsync(x => x.ID == task.ID);

            await _dbContext.TaskOrchestration.AddAsync(new TaskOrchestrationTable
            {
                AIIdentify = oldTask.AIIdentify,
                TaskName = oldTask.TaskName + $"Copy-{DateTime.Now.ToString("yyyyMMddHHmmss")}",
                TaskRule = oldTask.TaskRule,
                TaskSource = oldTask.TaskSource,
                GroupName = oldTask.GroupName,
                Priority = oldTask.Priority,
                UserDefinePromptsJson = oldTask.UserDefinePromptsJson,
                OperationMode = oldTask.OperationMode,
                TaskTarget = oldTask.TaskTarget,
                Filter = oldTask.Filter,
                IsEnabled = oldTask.IsEnabled,
                Shortcut = oldTask.Shortcut,
                IsRegex = oldTask.IsRegex,
                IsRelated = oldTask.IsRelated,
                AutomaticTable = null,
                CreateTime = DateTime.Now,
                Argument = oldTask.Argument,
                RuleType = oldTask.RuleType,
            });

            await _dbContext.SaveChangesAsync();
            await OnPageLoaded();
        }
    }

    public async Task OnTaskListCollectionChangedAsync(TaskOrchestrationTable task, int draggedIndex, int droppedIndex)
    {
        if ((DateTime.Now - LastInvocationTime).TotalMilliseconds < 800)
        {
            return; // 防止频繁触发
        }
        LastInvocationTime = DateTime.Now;

        if (draggedIndex < 0 || droppedIndex < 0 || draggedIndex == droppedIndex || draggedIndex >= TaskList.Count || droppedIndex >= TaskList.Count)
        {
            return;
        }
        TaskList.Move(draggedIndex, droppedIndex);

        for (int i = 0; i < TaskList.Count; i++)
        {
            TaskList[i].Priority = TaskList.Count - 1 - i;

            if (task.ID == TaskList[i].ID)
            {
                await QuartzHelper.UpdateTaskPriority($"{task.TaskName}#{task.ID}", task.GroupName.GroupName, TaskList[i].Priority);
            }
        }

        _dbContext.TaskOrchestration.UpdateRange(TaskList);
        await _dbContext.SaveChangesAsync();
        TaskListACV = new AdvancedCollectionView(TaskList, true);
        TaskListACV.SortDescriptions.Add(new SortDescription("Priority", SortDirection.Descending));
        TaskListACV.SortDescriptions.Add(new SortDescription("ID", SortDirection.Ascending));
    }

    /// <summary>
    /// 加载规则菜单
    /// </summary>
    private void LoadRulesMenu()
    {
        var orderedFolderRules = FolderRulesMenuDescription.ToList(); // 转为 List 保留顺序
        var orderedFileRules = FileRulesMenuDescription.ToList();
        MenuCategories =
        [
            new() {
                Title = "HandlingRulesForFiles".GetLocalized(),
                Items = orderedFileRules.Select(kv => kv.Value + " = " + kv.Key.GetLocalized()).ToList()
            },
            new() {
                Title = "HandlingFolderRules".GetLocalized(),
                Items = orderedFolderRules.Select(kv => kv.Value + " = " + kv.Key.GetLocalized()).ToList()
            }
        ];
    }

    private readonly Dictionary<string, string> FolderRulesMenuDescription = new Dictionary<string, string>
    {
        { "AllFoldersText","**" },
        { "AllFoldersWithoutOtherMatches","##" },
        { "AllFoldersStartingWithrobot","robot**" },
        { "AllFoldersContainingrobot","**robot**" },
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
        { "UseDelimiter","*.jpg;*.png" },
        { "UseDelimitersToExclude","*.jpg/sea*" },
        { "CompressedFile","*.7z;*.bz2;*.gz;*.iso;*.rar;*.xz;*.z;*.zip" },
        { "Document","*.djvu;*.doc;*.docx;*.epub;*.odt;*.pdf;*.rtp;*.tex;*.txt" },
        { "Photograph","*.bmp;*.gif;*.ico;*.jpg;*.jpeg;*.png;*.psd;*.tif;*.tiff" },
        { "Music","*.aac;*.flac;*.m4a;*mp3*.ogg;*.wma;*.wav" },
        { "PresentationAndWorksheet","*.csv;*.odp;*.ods;*.pps;*.ppt;*.pptx;*.xls;*.xlsx" },
        { "Video","*.avi;*.fly;*.m4v;*.mkv;*.mov;*.mp4;*.mpeg;*.mpg;*.wmv" },
    };

    private void InitializeRenameModel()
    {
        // 初始化日期时间方式
        DateTimeModel.Add(new PatternSnippetModel("#C", "AddAtTheEnd_Create".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("#M", "AddAtTheEnd_Modify".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("#S", "AddAtThenEnd_ShootingTime".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("#D", "AddAtThenEnd_NowTime".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$YYYY", "DateTimeCheatSheet_FullYear".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$YY", "DateTimeCheatSheet_YearLastTwoDigits".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$Y", "DateTimeCheatSheet_YearLastDigit".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$MMMM", "DateTimeCheatSheet_MonthName".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$MMM", "DateTimeCheatSheet_MonthNameAbbr".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$MM", "DateTimeCheatSheet_MonthDigitLZero".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$M", "DateTimeCheatSheet_MonthDigit".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$DDDD", "DateTimeCheatSheet_DayName".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$DDD", "DateTimeCheatSheet_DayNameAbbr".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$DD", "DateTimeCheatSheet_DayDigitLZero".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$D", "DateTimeCheatSheet_DayDigit".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$hh", "DateTimeCheatSheet_HoursLZero".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$h", "DateTimeCheatSheet_Hours".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$mm", "DateTimeCheatSheet_MinutesLZero".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$m", "DateTimeCheatSheet_Minutes".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$ss", "DateTimeCheatSheet_SecondsLZero".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$s", "DateTimeCheatSheet_Seconds".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$fff", "DateTimeCheatSheet_MilliSeconds3D".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$ff", "DateTimeCheatSheet_MilliSeconds2D".GetLocalized()));
        DateTimeModel.Add(new PatternSnippetModel("$f", "DateTimeCheatSheet_MilliSeconds1D".GetLocalized()));

        // 初始化计数器方式
        CounterModel.Add(new PatternSnippetModel("${}", "CounterCheatSheet_Simple".GetLocalized()));
        CounterModel.Add(new PatternSnippetModel("${start=10}", "CounterCheatSheet_Start".GetLocalized()));
        CounterModel.Add(new PatternSnippetModel("${increment=5}", "CounterCheatSheet_Increment".GetLocalized()));
        CounterModel.Add(new PatternSnippetModel("${padding=8}", "CounterCheatSheet_Padding".GetLocalized()));
        CounterModel.Add(new PatternSnippetModel("${increment=3,padding=4,start=900}", "CounterCheatSheet_Complex".GetLocalized()));

        // 初始化随机化方式
        RandomizerModel.Add(new PatternSnippetModel("${rstringalnum=9}", "RandomizerCheatSheet_Alnum".GetLocalized()));
        RandomizerModel.Add(new PatternSnippetModel("${rstringalpha=13}", "RandomizerCheatSheet_Alpha".GetLocalized()));
        RandomizerModel.Add(new PatternSnippetModel("${rstringdigit=36}", "RandomizerCheatSheet_Digit".GetLocalized()));
        RandomizerModel.Add(new PatternSnippetModel("${ruuidv4}", "RandomizerCheatSheet_Uuid".GetLocalized()));

        // 初始化替换方式
        ReplaceModel.Add(new PatternSnippetModel("${source}", "ReplaceCheatSheet_ReplaceSource".GetLocalized()));
        ReplaceModel.Add(new PatternSnippetModel("${parent}", "ReplaceCheatSheet_ReplaceParent".GetLocalized()));
        ReplaceModel.Add(new PatternSnippetModel("${replace=old,new,false}", "ReplaceCheatSheet_Replace".GetLocalized()));
        ReplaceModel.Add(new PatternSnippetModel("${replace=old,new,true}", "ReplaceCheatSheet_ReplaceIgnoreCase".GetLocalized()));
        ReplaceModel.Add(new PatternSnippetModel("${replace=old,,false}", "ReplaceCheatSheet_ReplaceDelete".GetLocalized()));
        ReplaceModel.Add(new PatternSnippetModel("${regex=^foo,new}", "ReplaceCheatSheet_ReplaceRegex".GetLocalized()));
        ReplaceModel.Add(new PatternSnippetModel("${regex=^foo,}", "ReplaceCheatSheet_ReplaceRegexDelete".GetLocalized()));
    }

}

