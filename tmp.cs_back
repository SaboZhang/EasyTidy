public partial class MainViewModel : ObservableObject, ITitleBarAutoSuggestBoxAware
{
    private readonly AppDbContext _dbContext;
    private FileSystemWatcher _watcher;

    private bool _isMonitoringEnabled;
    public bool IsMonitoringEnabled
    {
        get => _isMonitoringEnabled;
        set
        {
            SetProperty(ref _isMonitoringEnabled, value);
            if (value)
            {
                StartMonitoring();
            }
            else
            {
                StopMonitoring();
            }
        }
    }

    public MainViewModel(IJsonNavigationViewService jsonNavigationViewService, IThemeService themeService)
    {
        JsonNavigationViewService = jsonNavigationViewService;
        themeService.Initialize(App.MainWindow, true, Constants.CommonAppConfigPath);
        themeService.ConfigBackdrop();
        themeService.ConfigElementTheme();
        
        _dbContext = App.GetService<AppDbContext>();

        // 启动 OnStartupExecutionAsync，但不等待
        _ = Task.Run(OnStartupExecutionAsync);
    }

    private async Task OnStartupExecutionAsync()
    {
        try
        {
            var list = await _dbContext.Automatic.Include(a => a.TaskOrchestrationList)
                .Where(a => a.IsStartupExecution == true)
                .ToListAsync();

            foreach (var item in list)
            {
                foreach (var task in item.TaskOrchestrationList)
                {
                    // 执行操作
                    await OperationHandler.ExecuteOperationAsync(Convert.ToInt32(task.OperationMode), "示例参数1");
                }
            }
        }
        catch (Exception ex)
        {
            // 处理异常（例如记录日志）
            Debug.WriteLine($"Error in OnStartupExecutionAsync: {ex.Message}");
        }
    }

    private void StartMonitoring()
    {
        // 从数据库获取需要监控的文件路径
        var list = _dbContext.Automatic.Include(a => a.TaskOrchestrationList)
            .Where(a => a.IsStartupExecution == true)
            .ToList();

        foreach (var item in list)
        {
            foreach (var task in item.TaskOrchestrationList)
            {
                string folderPath = Path.GetDirectoryName(task.FilePath);
                string targetPath = "目标路径"; // 根据需求设置
                int delaySeconds = 1; // 适当的延迟时间
                FileOperationType fileOperationType = FileOperationType.YourDesiredOperation; // 根据需求设置

                MonitorFolder(folderPath, targetPath, delaySeconds, fileOperationType);
            }
        }
    }

    private void StopMonitoring()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    public static void MonitorFolder(string folderPath, string targetPath, int delaySeconds, FileOperationType fileOperationType)
    {
        if (_watcher != null) return; // 防止重复监控

        _watcher = new FileSystemWatcher
        {
            Path = folderPath,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
            Filter = "*.*"
        };

        _watcher.Created += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(e.FullPath, targetPath, fileOperationType));
        _watcher.Deleted += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(e.FullPath, targetPath, fileOperationType));
        _watcher.Changed += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(e.FullPath, targetPath, fileOperationType));
        _watcher.Renamed += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(e.FullPath, targetPath, fileOperationType));
        _watcher.EnableRaisingEvents = true;
    }

    private static void HandleFileChange(string fullPath, string targetPath, FileOperationType fileOperationType)
    {
        // 根据需要实现文件变动时的操作
        switch (fileOperationType)
        {
            case FileOperationType.Copy:
                File.Copy(fullPath, targetPath);
                break;
            case FileOperationType.Move:
                File.Move(fullPath, targetPath);
                break;
            // 添加其他操作
            default:
                break;
        }
    }
}
