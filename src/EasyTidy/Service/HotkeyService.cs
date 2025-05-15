using System;
using System.Runtime.InteropServices;
using Windows.System;
using WinRT.Interop;
using WinUIEx;

namespace EasyTidy.Service;

public partial class HotkeyService : IDisposable
{
    private readonly Dictionary<int, Action> _hotKeyActions = new();
    private readonly Dictionary<string, int> _hotKeyIds = new();
    private int _currentId = 1000;
    private IntPtr _hwnd;
    private DispatcherQueue _dispatcherQueue;
    private WindowEx _window;

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

    public bool TryParseGesture(string gesture, out VirtualKey key, out ModifierKeys modifiers)
    {
        key = VirtualKey.None;
        modifiers = ModifierKeys.None;

        if (string.IsNullOrWhiteSpace(gesture))
            return false;

        var parts = gesture.Split('+', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var p = part.Trim().ToUpper();
            if (p == "CTRL") modifiers |= ModifierKeys.Control;
            else if (p == "ALT") modifiers |= ModifierKeys.Alt;
            else if (p == "SHIFT") modifiers |= ModifierKeys.Shift;
            else if (p == "WIN") modifiers |= ModifierKeys.Win;
            else if (Enum.TryParse<VirtualKey>(p, out var parsedKey)) key = parsedKey;
        }

        return key != VirtualKey.None;
    }

    public static string ToGestureString(VirtualKey key, VirtualKeyModifiers modifiers)
    {
        var parts = new List<string>();

        if (modifiers.HasFlag(VirtualKeyModifiers.Control))
            parts.Add("Ctrl");
        if (modifiers.HasFlag(VirtualKeyModifiers.Menu))
            parts.Add("Alt");
        if (modifiers.HasFlag(VirtualKeyModifiers.Shift))
            parts.Add("Shift");
        if (modifiers.HasFlag(VirtualKeyModifiers.Windows))
            parts.Add("Win");

        parts.Add(key.ToString()); // 最后添加主键

        return string.Join(" + ", parts);
    }

}
