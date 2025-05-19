using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EasyTidy.Common.Extensions;
using EasyTidy.Common.Model;
using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using EasyTidy.Util;
using Windows.System;
using WinRT.Interop;
using WinUIEx;

namespace EasyTidy.Service;

public partial class HotkeyService(ILocalSettingsService localSettingsService, HotkeyActionRouter hotkeyActionRouter) : IDisposable
{
    private readonly Dictionary<int, Action> _hotKeyActions = new();
    private readonly Dictionary<string, int> _hotKeyIds = new();
    private int _currentId = 1000;
    private IntPtr _hwnd;
    private DispatcherQueue _dispatcherQueue;
    private WindowEx _window;

    private readonly ILocalSettingsService _localSettingsService = localSettingsService;

    private readonly HotkeyActionRouter _hotkeyActionRouter = hotkeyActionRouter;

    public void Initialize(WindowEx window)
    {
        _window = window;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _hwnd = WindowNative.GetWindowHandle(window);
        SubclassWndProc();
    }

    public bool RegisterHotKey(string id, VirtualKey key, ModifierKeys modifiers, Action callback)
    {
        if (_hotKeyIds.ContainsKey(id)) return false;

        int mod = (int)modifiers;
        int vk = (int)key;

        _currentId++;
        bool result = RegisterHotKey(_hwnd, _currentId, mod, vk);
        if (!result)
            return false;

        _hotKeyIds[id] = _currentId;
        _hotKeyActions[_currentId] = callback;
        return true;
    }

    public void UnregisterHotKey(string id)
    {
        if (_hotKeyIds.TryGetValue(id, out int hotkeyId))
        {
            UnregisterHotKey(_hwnd, hotkeyId);
            _hotKeyIds.Remove(id);
            _hotKeyActions.Remove(hotkeyId);
        }
    }

    public void Clear()
    {
        foreach (var id in _hotKeyIds.Values)
        {
            UnregisterHotKey(_hwnd, id);
        }
        _hotKeyIds.Clear();
        _hotKeyActions.Clear();
    }

    public void Dispose()
    {
        Clear();
        UnhookWndProc();
    }

    #region Native Interop

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private WndProcDelegate _newWndProc;
    private IntPtr _oldWndProc = IntPtr.Zero;

    private void SubclassWndProc()
    {
        _newWndProc = WndProc;
        _oldWndProc = SetWindowLongPtr(_hwnd, -4, Marshal.GetFunctionPointerForDelegate(_newWndProc));
    }

    private void UnhookWndProc()
    {
        if (_oldWndProc != IntPtr.Zero)
        {
            SetWindowLongPtr(_hwnd, -4, _oldWndProc);
            _oldWndProc = IntPtr.Zero;
        }
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_hotKeyActions.TryGetValue(id, out var action))
            {
                _dispatcherQueue.TryEnqueue(() => action?.Invoke());
            }
        }

        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [Flags]
    public enum ModifierKeys
    {
        None = 0x0000,
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Win = 0x0008
    }

    #endregion

    public static string ToGestureString(List<VirtualKey> keys)
    {
        if (keys == null || keys.Count == 0)
            return string.Empty;

        var nameMap = new Dictionary<VirtualKey, string>
        {
            [VirtualKey.Control] = "Ctrl",
            [VirtualKey.Menu] = "Alt",
            [VirtualKey.Shift] = "Shift",
            [VirtualKey.LeftWindows] = "Win",
            [VirtualKey.RightWindows] = "Win",
            [VirtualKey.Escape] = "Esc",
            [VirtualKey.Enter] = "Enter",
            [VirtualKey.CapitalLock] = "Caps Lock",
            [(VirtualKey)0xBC] = ",",
            [(VirtualKey)0xBE] = ".",
            [(VirtualKey)0xBA] = ";",
            [(VirtualKey)0xDE] = "'",
            [(VirtualKey)0xBF] = "/",
            [(VirtualKey)0xBD] = "-",
            [(VirtualKey)0xBB] = "=",
            [(VirtualKey)0xDB] = "[",
            [(VirtualKey)0xDD] = "]",
            [(VirtualKey)0xDC] = "\\",
            [(VirtualKey)0xC0] = "`",
        };

        // 修饰键排序优先级
        var modifierOrder = new List<VirtualKey>
        {
            VirtualKey.LeftWindows, VirtualKey.RightWindows,
            VirtualKey.Control,
            VirtualKey.Menu,
            VirtualKey.Shift,
        };

        var sortedKeys = keys
            .OrderBy(k =>
            {
                int index = modifierOrder.IndexOf(k);
                return index >= 0 ? index : modifierOrder.Count;
            })
            .ThenBy(k => k.ToString()) // 非修饰键按字母顺序
            .ToList();

        return string.Join(" + ", sortedKeys.Select(k =>
            nameMap.TryGetValue(k, out var alias) ? alias : k.ToString()));
    }

    public async Task ResetToDefaultHotkeysAsync()
    {
        // 读取当前用户快捷键配置
        var hotkeySettings = await _localSettingsService.LoadSettingsExtAsync<HotkeysCollection>();
        if (hotkeySettings == null)
            return;

        // 反注册所有已注册的快捷键
        foreach (var hotkey in hotkeySettings.Hotkeys)
        {
            UnregisterHotKey(hotkey.Id);
        }

        // 重置为默认快捷键（深拷贝 DefaultHotkeys.Hotkeys）
        hotkeySettings.Hotkeys = DefaultHotkeys.Hotkeys
        .Select(h => new Hotkey
        {
            Id = h.Id,
            KeyGesture = h.KeyGesture,
            CommandName = h.CommandName
        }).ToList();

        // 保存到配置文件
        await _localSettingsService.SaveSettingsExtAsync(hotkeySettings);

        // 重新注册默认快捷键
        foreach (var hotkey in hotkeySettings.Hotkeys)
        {
            if (TryParseHotkey(hotkey.KeyGesture, out var modifiers, out var mainKey))
            {
                RegisterHotKey(
                    hotkey.Id,
                    mainKey,
                    modifiers,
                    () => _hotkeyActionRouter.HandleAction(hotkey.CommandName)
                );
            }
        }
    }

    public bool TryParseHotkey(string hotkeyString, out ModifierKeys modifiers, out VirtualKey mainKey)
    {
        modifiers = ModifierKeys.None;
        mainKey = VirtualKey.None;

        if (string.IsNullOrWhiteSpace(hotkeyString))
            return false;

        var parts = hotkeyString.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            switch (part.ToLower())
            {
                case "ctrl":
                case "control":
                    modifiers |= ModifierKeys.Control;
                    break;
                case "alt":
                    modifiers |= ModifierKeys.Alt;
                    break;
                case "shift":
                    modifiers |= ModifierKeys.Shift;
                    break;
                case "win":
                case "windows":
                    modifiers |= ModifierKeys.Win;
                    break;
                default:
                    if (Enum.TryParse<VirtualKey>(part, true, out var parsedKey))
                    {
                        if (mainKey == VirtualKey.None)
                            mainKey = parsedKey;
                    }
                    else
                    {
                        return false; // 解析失败
                    }
                    break;
            }
        }

        return mainKey != VirtualKey.None;
    }

}
