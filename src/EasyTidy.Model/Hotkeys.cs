using System;
using System.Collections.Generic;

namespace EasyTidy.Model;

public class Hotkey
{
    public string Id { get; set; } = string.Empty;
    public string KeyGesture { get; set; } = string.Empty; // 示例："Ctrl+S"
    public string CommandName { get; set; } = string.Empty; // ViewModel 中的命令名
}

public class HotkeysCollection
{
    public List<Hotkey> Hotkeys { get; set; } = new();
}
