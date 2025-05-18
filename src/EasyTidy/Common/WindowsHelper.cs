using EasyTidy.Common.Extensions;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices;
using WinUIEx;

namespace EasyTidy.Common;

public class WindowsHelper
{
    private const int WindowWidth = 300;
    private const int WindowHeight = 340;

    private const int GWLP_WNDPROC = -4;
    private const int WM_NCHITTEST = 0x0084;

    private static IntPtr _oldWndProc = IntPtr.Zero;
    private static WndProc? _newWndProc = null;
    private static bool _isPinned = false;

    private static IntPtr _hwnd = IntPtr.Zero;

    public delegate IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr newProc);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    public static void PositionWindowBottomRight(WindowEx window)
    {
        // 获取主窗口信息
        var mainHwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(mainHwnd);
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;

        // 获取缩放比例
        var dpi = GetDpiForWindow(mainHwnd);
        double scale = dpi / 96.0;

        // 缩放后尺寸
        int scaledWidth = (int)((WindowWidth + 20) * scale);
        int scaledHeight = (int)((WindowHeight + 20) * scale);

        // 计算右下角位置
        int x = workArea.X + workArea.Width - scaledWidth;
        int y = workArea.Y + workArea.Height - scaledHeight;

        window.MoveAndResize(x, y, WindowWidth, WindowHeight);
    }

    public static void SetWindowStyle(WindowEx window)
    {
        window.Title = "EasyTidy Drag and drop";
        window.ExtendsContentIntoTitleBar = true;
        window.IsMaximizable = false;
        window.MaxWidth = WindowWidth;
        window.MaxHeight = WindowHeight;
        window.SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };
    }

    public static void EnsureChildWindow()
    {
        if (App.ChildWindow == null || App.ChildWindow.IsClosed())
        {
            App.ChildWindow = new WindowEx();
        }
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    /// <summary>
    /// 初始化拖动控制（只需调用一次）
    /// </summary>
    public static void Initialize(IntPtr hwnd)
    {
        if (_hwnd != IntPtr.Zero) return;

        _hwnd = hwnd;
        _newWndProc = new WndProc(WndProcOverride);
        _oldWndProc = SetWindowLongPtr(_hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));
    }

    /// <summary>
    /// 设置是否允许拖动
    /// </summary>
    public static void SetPinned(bool pinned)
    {
        _isPinned = pinned;
    }

    private static IntPtr WndProcOverride(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
    {
        if (_isPinned && msg == WM_NCHITTEST)
        {
            // 返回 HTCLIENT 表示点击的是窗口客户区，不允许拖动
            return new IntPtr(1); // HTCLIENT
        }

        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    public static void SetWindowContent(WindowEx window)
    {
        window.Content = new MainPage();
    }
}
