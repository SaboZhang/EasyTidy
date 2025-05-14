using System;
using System.Runtime.InteropServices;

namespace EasyTidy.Common;

public class HotkeyUtility
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int HOTKEY_ID_TEST = 0xB123; // 临时ID，可随意设定

    public static bool IsHotkeyAvailable(uint modifiers, uint key)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        if (RegisterHotKey(hwnd, HOTKEY_ID_TEST, modifiers, key))
        {
            UnregisterHotKey(hwnd, HOTKEY_ID_TEST); // 注册成功立即释放
            return true;
        }

        return false; // 注册失败说明被系统或其他软件占用
    }
}
