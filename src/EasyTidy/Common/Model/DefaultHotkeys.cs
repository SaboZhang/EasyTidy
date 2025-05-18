using System;
using EasyTidy.Model;
using EasyTidy.Service;
using Windows.System;

namespace EasyTidy.Common.Model;

public static class DefaultHotkeys
{
    public static readonly List<Hotkey> Hotkeys = new()
    {
        new Hotkey
        {
            Id = "ToggleChildWindow",
            KeyGesture = HotkeyService.ToGestureString([VirtualKey.Menu, VirtualKey.D]),
            CommandName = "ToggleChildWindow"
        },
        new Hotkey
        {
            Id = "ToggleSettingsWindow",
            KeyGesture = HotkeyService.ToGestureString([VirtualKey.Control, (VirtualKey)188]),
            CommandName = "ToggleSettingsWindow"
        },
        new Hotkey
        {
            Id = "ShowMainWindow",
            KeyGesture = HotkeyService.ToGestureString([VirtualKey.Control, VirtualKey.W]),
            CommandName = "ShowMainWindow"
        },
        new Hotkey
        {
            Id = "ExitApp",
            KeyGesture = HotkeyService.ToGestureString([VirtualKey.Menu,VirtualKey.Shift ,VirtualKey.Q]),
            CommandName = "ExitApp"
        },
        new Hotkey
        {
            Id = "ExecuteAllTasks",
            KeyGesture = HotkeyService.ToGestureString([VirtualKey.F8]),
            CommandName = "ExecuteAllTasks"
        }
        // 🔥 以后只需要在这里加新快捷键就行
    };

    public static Hotkey? GetHotkeyById(string id)
    {
        return Hotkeys.FirstOrDefault(h => h.Id == id);
    }

}
