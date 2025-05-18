using System;
using System.Collections.Generic;
using Windows.System;

namespace EasyTidy.Model;

public class Hotkey
{
    public string Id { get; set; } = string.Empty;
    public string KeyGesture { get; set; } = string.Empty; // 示例："Ctrl+S"
    public string CommandName { get; set; } = string.Empty; // ViewModel 中的命令名
    public List<VirtualKey> keys { get; set; } = new();
}

public class HotkeysCollection
{
    public List<Hotkey> Hotkeys { get; set; } = new();
}
