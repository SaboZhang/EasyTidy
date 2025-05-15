using System;
using System.Runtime.InteropServices;

namespace EasyTidy.Common;

public class HotkeyActionRouter
{
    private readonly Dictionary<string, Action> _actionMap = new()
    {
        ["ToggleChildWindow"] = () => MainViewModel.Instance?.ToggleChildWindow(),
        ["ExitApp"] = () => Application.Current.Exit(),
    };

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
}
