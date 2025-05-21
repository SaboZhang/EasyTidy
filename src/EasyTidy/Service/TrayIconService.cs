using System;
using H.NotifyIcon;
using EasyTidy.Model;
using Microsoft.UI.Xaml.Media.Imaging;
using CommunityToolkit.WinUI;

namespace EasyTidy.Service;

public class TrayIconService
{
    private static TaskbarIcon _trayIcon;

    private static readonly Dictionary<TrayIconStatus, string> IconPaths = new()
    {
        { TrayIconStatus.Normal,  "ms-appx:///Assets/Inactive.ico" },
        { TrayIconStatus.Running, "ms-appx:///Assets/Red.ico" },
        { TrayIconStatus.Paused,  "ms-appx:///Assets/Yellow.ico" },
        { TrayIconStatus.Error,   "ms-appx:///Assets/Error.ico" },
        { TrayIconStatus.HotKey,  "ms-appx:///Assets/hotkey.ico" },
    };

    private static TrayIconStatus _currentStatus = TrayIconStatus.Normal;

    public static void Initialize(TaskbarIcon trayIcon)
    {
        _trayIcon = trayIcon ?? throw new ArgumentNullException(nameof(trayIcon));
        ApplyIcon(TrayIconStatus.Normal); // 设置初始状态
    }

    public static void SetStatus(TrayIconStatus status, string? toolTipKey = null)
    {
        if (_trayIcon == null)
            throw new InvalidOperationException("TrayIconManager not initialized.");

        if (_currentStatus == status)
            return; // 避免重复设置相同状态

        _currentStatus = status;

        if (IconPaths.TryGetValue(status, out var iconPath))
        {
            _trayIcon.IconSource = new BitmapImage(new Uri(iconPath));
        }
        if (!string.IsNullOrEmpty(toolTipKey))
        {
            _trayIcon.ToolTipText = toolTipKey.GetLocalized();
        }
    }

    public static TrayIconStatus CurrentStatus => _currentStatus;

    public static void UpdateIconPath(TrayIconStatus status, string newPath)
    {
        if (!string.IsNullOrWhiteSpace(newPath))
        {
            IconPaths[status] = newPath;
        }
    }

    /// <summary>
    /// 应用图标到托盘控件。
    /// </summary>
    private static void ApplyIcon(TrayIconStatus status)
    {
        if (IconPaths.TryGetValue(status, out var iconPath))
        {
            try
            {
                _trayIcon.IconSource = new BitmapImage(new Uri(iconPath));
                _trayIcon.ToolTipText = $"EasyTidy {Constants.Version}"; // 设置默认提示文本
            }
            catch (Exception ex)
            {
                // 可选：添加日志记录或调试信息
                System.Diagnostics.Debug.WriteLine($"[TrayIcon] 设置图标失败: {ex.Message}");
            }
        }
    }
}
