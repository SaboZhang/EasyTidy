using CommunityToolkit.WinUI;
using EasyTidy.Activation;
using EasyTidy.Common.Database;
using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using Microsoft.EntityFrameworkCore;
using System.Data;
using WinUIEx;

namespace EasyTidy.Service;

public class ActivationService : IActivationService
{
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler;
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly IThemeSelectorService _themeSelectorService;
    private UIElement? _shell = null;
    private readonly AppDbContext _dbContext;

    public ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler, IEnumerable<IActivationHandler> activationHandlers, IThemeSelectorService themeSelectorService)
    {
        _defaultHandler = defaultHandler;
        _activationHandlers = activationHandlers;
        _themeSelectorService = themeSelectorService;
        _dbContext = App.GetService<AppDbContext>();
    }

    public async Task ActivateAsync(object activationArgs)
    {
        // Execute tasks before activation.
        await InitializeAsync();

        // Set the MainWindow Content.
        if (App.MainWindow.Content == null)
        {
            _shell = App.GetService<ShellPage>();
            App.MainWindow.Content = _shell ?? new Frame();
        }

        // Activate the MainWindow.
        SetWindowBehavior();
        // App.MainWindow.Activate();

        // Execute tasks after activation.
        await _dbContext.InitializeDatabaseAsync();
        await StartupAsync();
        await PerformStartupChecksAsync();

        // Handle activation via ActivationHandlers.
        await HandleActivationAsync(activationArgs);
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        var activationHandler = _activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs));

        if (activationHandler != null)
        {
            await activationHandler.HandleAsync(activationArgs);
        }

        if (_defaultHandler.CanHandle(activationArgs))
        {
            await _defaultHandler.HandleAsync(activationArgs);
        }
    }

    private async Task InitializeAsync()
    {
        await _themeSelectorService.InitializeAsync().ConfigureAwait(false);
        await Task.CompletedTask;
    }

    private async Task StartupAsync()
    {
        // _themeSelectorService.ThemeChanged += (_, theme) => App.MainWindow.SetRequestedTheme(theme);
        await _themeSelectorService.SetRequestedThemeAsync();
        OnStartupExecutionAsync();
        OnStartAllMonitoring();
        await Task.CompletedTask;
    }

    private async Task PerformStartupChecksAsync()
    {
        if ((bool)Settings.GeneralConfig.IsStartupCheck)
        {
            var app = App.GetService<AppUpdateSettingViewModel>();
            await app.CheckForNewVersionAsync();
        }

        await QuartzConfig.InitQuartzConfigAsync();
        await QuartzHelper.StartAllJob();
    }

    private void SetWindowBehavior()
    {
        App.MainWindow.Closed += (sender, args) =>
        {
            if (App.HandleClosedEvents)
            {
                args.Handled = true;
                App.MainWindow.Hide();
            }
        };

        if ((bool)Settings.GeneralConfig.Minimize)
        {
            // MainWindow.Activate();
            App.MainWindow.Hide();
        }
        else
        {
            _themeSelectorService.ApplyTheme();
            App.MainWindow.Activate();
        }
    }

    /// <summary>
    /// 启动时执行
    /// </summary>
    /// <returns></returns>
    private void OnStartupExecutionAsync()
    {
        try
        {
            var list = _dbContext.Automatic.Include(a => a.TaskOrchestrationList).Where(a => a.IsStartupExecution).ToList();
            if (list.Count == 0) return;

            // 并行执行任务，但不等待
            var tasks = list.SelectMany(item =>
                item.TaskOrchestrationList
                    .Where(t => t.IsRelated == true)
                    .Select(async task =>
                    {

                        // 执行操作
                        await OperationHandler.ExecuteOperationAsync(task.OperationMode, new OperationParameters(
                            task.OperationMode,
                            task.TaskSource = task.TaskSource.Equals("DesktopText".GetLocalized())
                            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : task.TaskSource,
                            task.TaskTarget,
                            Settings.GeneralConfig.FileOperationType,
                            (bool)Settings.GeneralConfig.SubFolder,
                            new List<Func<string, bool>>(FilterUtil.GeneratePathFilters(task.TaskRule, task.RuleType)),
                            FilterUtil.GetPathFilters(task.Filter),
                            new RuleModel()
                            {
                                Rule = task.TaskRule,
                                RuleType = task.RuleType,
                                Filter = task.Filter
                            })
                        {
                            CreateTime = task.CreateTime,
                            Priority = task.Priority
                        });
                    }));

            // 启动所有任务，但不等待它们完成
            _ = Task.WhenAll(tasks);
            Logger.Info($"启动{list.Count}个任务成功（启动时执行的任务）....");
        }
        catch (Exception ex)
        {
            Logger.Error($"启动时执行失败：{ex}");
        }

    }

    /// <summary>
    /// 启动所有文件夹监控
    /// </summary>
    private void OnStartAllMonitoring()
    {
        try
        {
            var list = _dbContext.Automatic.Include(a => a.TaskOrchestrationList).Where(a => a.IsFileChange == true).ToList();
            if (list.Count == 0) return;
            foreach (var item in list)
            {
                foreach (var task in item.TaskOrchestrationList.Where(t => t.IsRelated == true))
                {
                    RuleModel rule = new()
                    {
                        Rule = task.TaskRule,
                        RuleType = task.RuleType,
                        Filter = task.Filter
                    };
                    // 根据 GeneralConfig 判断调用
                    FileOperationType fileOperationType = Settings.GeneralConfig != null
                        ? Settings.GeneralConfig.FileOperationType
                        : default; // 使用 default 或者枚举中的某个默认值
                    // 执行操作
                    var sub = Settings.GeneralConfig?.SubFolder ?? false;
                    var parameters = new OperationParameters(
                        task.OperationMode,
                        task.TaskSource,
                        task.TaskTarget,
                        fileOperationType,
                        sub,
                        FilterUtil.GeneratePathFilters(task.TaskRule, task.RuleType),
                        FilterUtil.GetPathFilters(task.Filter),
                        rule)
                    {
                        CreateTime = task.CreateTime,
                        Priority = task.Priority
                    };
                    Task.Run(() =>
                    {
                        FileEventHandler.MonitorFolder(parameters, Convert.ToInt32(string.IsNullOrEmpty(item.DelaySeconds) ? "5" : item.DelaySeconds));
                    });
                }
            }
            Logger.Info($"启动{list.Count}个监控任务成功....");
        }
        catch (Exception ex)
        {
            Logger.Error($"启动监控失败：{ex}");
        }
    }
}
