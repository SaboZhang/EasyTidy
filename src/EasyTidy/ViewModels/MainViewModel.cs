using EasyTidy.Common.Database;
using EasyTidy.Model;
using EasyTidy.Service;
using Microsoft.EntityFrameworkCore;
using WinRT;

namespace EasyTidy.ViewModels;
public partial class MainViewModel : ObservableObject, ITitleBarAutoSuggestBoxAware
{
    private readonly AppDbContext _dbContext;

    public IJsonNavigationViewService JsonNavigationViewService;
    public MainViewModel(IJsonNavigationViewService jsonNavigationViewService, IThemeService themeService)
    {
        JsonNavigationViewService = jsonNavigationViewService;
        themeService.Initialize(App.MainWindow, true, Constants.CommonAppConfigPath);
        themeService.ConfigBackdrop();
        themeService.ConfigElementTheme();
        _dbContext = App.GetService<AppDbContext>();
        // 启动时执行，不等待
        OnStartupExecutionAsync();
        OnStartAllMonitoring();
    }

    public void OnAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {

    }

    public void OnAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {

    }

    /// <summary>
    /// 启动时执行
    /// </summary>
    /// <returns></returns>
    private void OnStartupExecutionAsync()
    {
        var list = _dbContext.Automatic.Include(a => a.TaskOrchestrationList).Where(a => a.IsStartupExecution == true).ToList();
        // 并行执行任务，但不等待
        var tasks = list.SelectMany(item =>
            item.TaskOrchestrationList
                .Where(t => t.IsRelated)
                .Select(task => Task.Run(async () =>
                    await OperationHandler.ExecuteOperationAsync(task.OperationMode, new OperationParameters
                    {
                        SourcePath = "示例参数1", // 示例参数
                        TargetPath = "示例参数2"  // 可以根据需要修改
                    }))));

        // 启动所有任务，但不等待它们完成
        _ = Task.WhenAll(tasks);

    }

    /// <summary>
    /// 启动所有文件夹监控
    /// </summary>
    private void OnStartAllMonitoring()
    {
        var list = _dbContext.Automatic.Include(a => a.TaskOrchestrationList).Where(a => a.RegularTaskRunning == true).ToList();
        foreach (var item in list)
        {
            foreach (var task in item.TaskOrchestrationList.Where(t => t.IsRelated))
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
                FileEventHandler.MonitorFolder(task.OperationMode, task.TaskSource, task.TaskTarget, Convert.ToInt32(item.DelaySeconds), rule, fileOperationType);
                
            }
        }
    }
}
