﻿using CommunityToolkit.WinUI;
using EasyTidy.Common.Database;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.Util;
using Microsoft.EntityFrameworkCore;

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
        try
        {
            var list = _dbContext.Automatic.Include(a => a.TaskOrchestrationList).Where(a => a.IsStartupExecution).ToList();

            // 并行执行任务，但不等待
            var tasks = list.SelectMany(item =>
                item.TaskOrchestrationList
                    .Where(t => t.IsRelated)
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
                            FilterUtil.GetPathFilters(task.Filter))
                        {
                            CreateTime = task.CreateTime,
                            Priority = task.Priority
                        });
                    }));

            // 启动所有任务，但不等待它们完成
            _ = Task.WhenAll(tasks);
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
            Dictionary<string, List<string>> sourceToTargetsCache = new Dictionary<string, List<string>>();
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
        }
        catch (Exception ex)
        {
            Logger.Error($"启动监控失败：{ex}");
        }
    }
}
