using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EasyTidy.Common.Extensions;
using EasyTidy.Common.Model;
using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using Windows.System;
using WinRT.Interop;
using WinUIEx;

namespace EasyTidy.Service;

public partial class HotkeyService(ILocalSettingsService localSettingsService, HotkeyActionRouter hotkeyActionRouter) : IDisposable
{
    // 新增字段
    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
    private IntPtr _hookID = IntPtr.Zero;
    private HookProc _hookProc;

    // 当前按下的键集合
    private readonly HashSet<VirtualKey> _pressedKeys = new();

    // 多键组合注册表：id => 组合键
    private readonly Dictionary<string, List<VirtualKey>> _multiKeyHotkeys = new();

    // id 对应回调
    private readonly Dictionary<string, Action> _multiKeyActions = new();
    private readonly HashSet<string> _triggeredHotkeyIds = new(); // 防重复触发

    // Windows消息，键盘事件
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

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
        ClearMultiKeyHotkeys();
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

    // 需要的API导入
    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private const int WH_KEYBOARD_LL = 13;

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

    public bool TryParseGesture(string gesture, out List<VirtualKey> keys)
    {
        keys = new List<VirtualKey>();

        if (string.IsNullOrWhiteSpace(gesture))
            return false;

        // 快捷键别名映射（大写形式）
        var aliasMap = new Dictionary<string, VirtualKey>(StringComparer.OrdinalIgnoreCase)
        {
            { "CTRL", VirtualKey.Control },
            { "CONTROL", VirtualKey.Control },
            { "ALT", VirtualKey.Menu },
            { "SHIFT", VirtualKey.Shift },
            { "WIN", VirtualKey.LeftWindows },
            { "WINDOWS", VirtualKey.LeftWindows },
            { "RWIN", VirtualKey.RightWindows },
            { "ESC", VirtualKey.Escape },
            { "ENTER", VirtualKey.Enter },
            { "CAPS LOCK", VirtualKey.CapitalLock },
            { "CAPSLOCK", VirtualKey.CapitalLock },
            { "←", VirtualKey.Left },
            { "LEFT", VirtualKey.Left },
            { "→", VirtualKey.Right },
            { "RIGHT", VirtualKey.Right },
            { "↑", VirtualKey.Up },
            { "UP", VirtualKey.Up },
            { "↓", VirtualKey.Down },
            { "DOWN", VirtualKey.Down },

            // 标点符号
            { ",", (VirtualKey)0xBC },    // VK_OEM_COMMA
            { ".", (VirtualKey)0xBE },    // VK_OEM_PERIOD
            { ";", (VirtualKey)0xBA },    // VK_OEM_1
            { "\"", (VirtualKey)0xDE },   // VK_OEM_7
            { "/", (VirtualKey)0xBF },    // VK_OEM_2
            { "-", (VirtualKey)0xBD },    // VK_OEM_MINUS
            { "=", (VirtualKey)0xBB },    // VK_OEM_PLUS（注意：是等号键）
            { "[", (VirtualKey)0xDB },    // VK_OEM_4
            { "]", (VirtualKey)0xDD },    // VK_OEM_6
            { "\\", (VirtualKey)0xDC },   // VK_OEM_5
            { "`", (VirtualKey)0xC0 },    // VK_OEM_3（反引号）
            { "~", (VirtualKey)0xC0 }     // 同 VK_OEM_3（带 Shift）
        };


        var parts = gesture.Split('+', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var trimmed = part.Trim();

            if (aliasMap.TryGetValue(trimmed, out var mappedKey))
            {
                if (!keys.Contains(mappedKey))
                    keys.Add(mappedKey);
            }
            else if (Enum.TryParse<VirtualKey>(trimmed, true, out var parsedKey))
            {
                if (!keys.Contains(parsedKey))
                    keys.Add(parsedKey);
            }
            else
            {
                return false; // 有一个无效就直接失败
            }
        }

        return keys.Count > 0;
    }

    public static string ToGestureString(List<VirtualKey> keys)
    {
        if (keys == null || keys.Count == 0)
            return string.Empty;

        // 将 VirtualKey 转成字符串表示（比如 VirtualKey.Control -> Ctrl）
        var nameMap = new Dictionary<VirtualKey, string>
        {
            [VirtualKey.Control] = "Ctrl",
            [VirtualKey.Menu] = "Alt",
            [VirtualKey.Shift] = "Shift",
            [VirtualKey.LeftWindows] = "Win",
            [VirtualKey.RightWindows] = "Win",
            // 功能键
            [VirtualKey.Escape] = "Esc",
            [VirtualKey.Enter] = "Enter",
            [VirtualKey.CapitalLock] = "Caps Lock",

            // 标点符号（VirtualKey 是 ushort 类型，需要强转）
            [(VirtualKey)0xBC] = ",",
            [(VirtualKey)0xBE] = ".",
            [(VirtualKey)0xBA] = ";", // 也可视情况显示为 ":" + Shift
            [(VirtualKey)0xDE] = "'",
            [(VirtualKey)0xBF] = "/",
            [(VirtualKey)0xBD] = "-",
            [(VirtualKey)0xBB] = "=",
            [(VirtualKey)0xDB] = "[",
            [(VirtualKey)0xDD] = "]",
            [(VirtualKey)0xDC] = "\\",
            [(VirtualKey)0xC0] = "`",
        };

        return string.Join(" + ", keys.Select(k =>
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
            UnregisterMultiKeyHotkey(hotkey.Id);
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
            _ = TryParseGesture(hotkey.KeyGesture, out var keys);
            RegisterMultiKeyHotkey(
                hotkey.Id,
                keys,
                () => _hotkeyActionRouter.HandleAction(hotkey.CommandName)
            );
        }
    }

    // Hook结构体 (KBDLLHOOKSTRUCT)
    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    // 启动钩子
    private void StartKeyboardHook()
    {
        _hookProc = HookCallback;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        IntPtr moduleHandle = GetModuleHandle(null);
        _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, moduleHandle, 0);
        Debug.WriteLine($"_hookID = {_hookID}");
    }

    // 卸载钩子
    private void StopKeyboardHook()
    {
        Debug.WriteLine($"_hookID = {_hookID}");
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int wParamInt = wParam.ToInt32();
            var kbData = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var key = (VirtualKey)kbData.vkCode;

            if (wParamInt == WM_KEYDOWN || wParamInt == WM_SYSKEYDOWN)
            {
                // 只有首次按下时触发
                if (!_pressedKeys.Contains(key))
                {
                    _pressedKeys.Add(key);
                    Debug.WriteLine($"KeyDown: {key}, vkCode: {kbData.vkCode}, pressed keys: {string.Join(",", _pressedKeys)}");
                    CheckMultiKeyHotkeys();
                }
            }
            else if (wParamInt == WM_KEYUP || wParamInt == WM_SYSKEYUP)
            {
                _pressedKeys.Remove(key);
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    // 判断当前按下键是否触发组合键
    private void CheckMultiKeyHotkeys()
    {
        var normalizedPressedKeys = GetNormalizedPressedKeys().ToList();

        foreach (var kvp in _multiKeyHotkeys)
        {
            var hotkeyId = kvp.Key;
            var comboKeys = kvp.Value;

            Debug.WriteLine($"[CheckMultiKeyHotkeys] HotkeyId: {hotkeyId}, ComboKeys: {string.Join(",", comboKeys)}");

            bool allContained = comboKeys.All(k => normalizedPressedKeys.Contains(k));
            bool countMatch = comboKeys.Count == normalizedPressedKeys.Count;

            Debug.WriteLine($"HotkeyId: {hotkeyId}, ComboKeys: {string.Join(",", comboKeys)}, PressedKeys: {string.Join(",", _pressedKeys)}");
            Debug.WriteLine($"AllContained: {allContained}, CountMatch: {countMatch}");

            // 判断：是否完全匹配（所有键都按下，且数量一致）
            if (allContained && countMatch)
            {
                // 避免重复触发
                if (!_triggeredHotkeyIds.Contains(hotkeyId))
                {
                    _triggeredHotkeyIds.Add(hotkeyId);

                    if (_multiKeyActions.TryGetValue(hotkeyId, out var action))
                    {
                        _dispatcherQueue?.TryEnqueue(() => action?.Invoke());
                    }
                }
            }
            else
            {
                // 组合键未完整按下，移除触发标记，允许下次再触发
                _triggeredHotkeyIds.Remove(hotkeyId);
            }
        }
    }

    private VirtualKey NormalizeKey(VirtualKey key)
    {
        return key switch
        {
            VirtualKey.LeftControl or VirtualKey.RightControl => VirtualKey.Control,
            VirtualKey.LeftShift or VirtualKey.RightShift => VirtualKey.Shift,
            VirtualKey.LeftMenu or VirtualKey.RightMenu => VirtualKey.Menu, // Alt
            VirtualKey.LeftWindows or VirtualKey.RightWindows => VirtualKey.LeftWindows, // 或用一个统一标识，比如 LeftWindows
            _ => key,
        };
    }

    private IEnumerable<VirtualKey> GetNormalizedPressedKeys()
    {
        return _pressedKeys.Select(k => NormalizeKey(k)).Distinct();
    }

    // 新的注册多键快捷键方法
    public bool RegisterMultiKeyHotkey(string id, List<VirtualKey> keys, Action callback)
    {
        if (_multiKeyHotkeys.ContainsKey(id))
            return false;

        if (keys == null || keys.Count == 0)
            return false;

        _multiKeyHotkeys[id] = keys;
        _multiKeyActions[id] = callback;

        Debug.WriteLine($"[RegisterMultiKeyHotkey] HotkeyId: {id}, ComboKeys: {string.Join(", ", keys)}");

        // 启动钩子（第一次注册时）
        if (_hookID == IntPtr.Zero)
        {
            StartKeyboardHook();
        }

        return true;
    }

    // 卸载多键快捷键
    public void UnregisterMultiKeyHotkey(string id)
    {
        if (_multiKeyHotkeys.Remove(id))
        {
            _multiKeyActions.Remove(id);
        }

        // 如果没有剩余的多键快捷键，卸载钩子释放资源
        if (_multiKeyHotkeys.Count == 0)
        {
            StopKeyboardHook();
        }
    }

    // 清除所有多键快捷键
    public void ClearMultiKeyHotkeys()
    {
        _multiKeyHotkeys.Clear();
        _multiKeyActions.Clear();
        StopKeyboardHook();
    }

}
