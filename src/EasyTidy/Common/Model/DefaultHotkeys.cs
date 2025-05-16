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
            KeyGesture = HotkeyService.ToGestureString(VirtualKey.D, VirtualKeyModifiers.Menu),
            CommandName = "ToggleChildWindow"
        }
        // 🔥 以后只需要在这里加新快捷键就行
    };
}
