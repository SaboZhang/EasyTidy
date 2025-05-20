using EasyTidy.Contracts.Service;
using EasyTidy.Service;
using System;
using System.Runtime.InteropServices;

namespace EasyTidy.Common;

public class HotkeyActionRouter
{
    private readonly Dictionary<string, Action> _actionMap;

    private readonly INavigationService navigationService;

    public HotkeyActionRouter()
    {
        navigationService = App.GetService<INavigationService>();
        _actionMap = new Dictionary<string, Action>
        {
            ["ToggleChildWindow"] = () => MainViewModel.Instance?.ToggleChildWindow(),
            ["ExitApp"] = ExitApp,
            ["ToggleSettingsWindow"] = ToggleSettingsWindow,
            ["ShowMainWindow"] = () => App.MainWindow.Activate(),
            ["ExecuteAllTasks"] = ExecuteAllTasks,
        };
    }

    public void HandleAction(string actionName)
    {
        if (_actionMap.TryGetValue(actionName, out var action))
        {
            action?.Invoke();
        }
        else
        {
            Logger.Error($"Action '{actionName}' not found in action map.");
        }
    }

    private void ToggleSettingsWindow()
    {
        App.MainWindow.Activate();
        navigationService.NavigateTo(typeof(SettingsViewModel).FullName!);
    }

    private async void ExecuteAllTasks()
    {
        await QuartzHelper.TriggerAllJobsOnceAsync();
        await MainViewModel.Instance?.ExecuteAllTaskAsync();
    }

    private void ExitApp()
    {
        try
        {
            App._mutex?.ReleaseMutex();
            App.HandleClosedEvents = false;
            TrayIconView.Instance?.DisposeTrayIcon();
            App.ChildWindow?.Close();
            App.MainWindow?.Close();
        }
        catch
        {
            Environment.Exit(0);
        }
    }
}
