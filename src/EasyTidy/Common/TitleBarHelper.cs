using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices;
using Windows.UI;
using Windows.UI.ViewManagement;
using WinUIEx;

namespace EasyTidy.Common;

public class TitleBarHelper
{
    private const int WAINACTIVE = 0x00;
    private const int WAACTIVE = 0x01;
    private const int WMACTIVATE = 0x0006;

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

    public static void UpdateTitleBar(ElementTheme theme)
    {
        if (App.MainWindow.ExtendsContentIntoTitleBar)
        {
            if (theme == ElementTheme.Default)
            {
                var uiSettings = new UISettings();
                var background = uiSettings.GetColorValue(UIColorType.Background);

                theme = background == Microsoft.UI.Colors.White ? ElementTheme.Light : ElementTheme.Dark;
            }

            if (theme == ElementTheme.Default)
            {
                theme = Application.Current.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark;
            }

            App.MainWindow.AppWindow.TitleBar.ButtonForegroundColor = theme switch
            {
                ElementTheme.Dark => Microsoft.UI.Colors.White,
                ElementTheme.Light => Microsoft.UI.Colors.Black,
                _ => Microsoft.UI.Colors.Transparent
            };

            App.MainWindow.AppWindow.TitleBar.ButtonHoverForegroundColor = theme switch
            {
                ElementTheme.Dark => Microsoft.UI.Colors.White,
                ElementTheme.Light => Microsoft.UI.Colors.Black,
                _ => Microsoft.UI.Colors.Transparent
            };

            App.MainWindow.AppWindow.TitleBar.ButtonHoverBackgroundColor = theme switch
            {
                ElementTheme.Dark => Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF),
                ElementTheme.Light => Color.FromArgb(0x33, 0x00, 0x00, 0x00),
                _ => Microsoft.UI.Colors.Transparent
            };

            App.MainWindow.AppWindow.TitleBar.ButtonPressedBackgroundColor = theme switch
            {
                ElementTheme.Dark => Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF),
                ElementTheme.Light => Color.FromArgb(0x66, 0x00, 0x00, 0x00),
                _ => Microsoft.UI.Colors.Transparent
            };

            App.MainWindow.AppWindow.TitleBar.BackgroundColor = Microsoft.UI.Colors.Transparent;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            if (hwnd == GetActiveWindow())
            {
                SendMessage(hwnd, WMACTIVATE, WAINACTIVE, IntPtr.Zero);
                SendMessage(hwnd, WMACTIVATE, WAACTIVE, IntPtr.Zero);
            }
            else
            {
                SendMessage(hwnd, WMACTIVATE, WAACTIVE, IntPtr.Zero);
                SendMessage(hwnd, WMACTIVATE, WAINACTIVE, IntPtr.Zero);
            }
        }
    }

    public static void ApplySystemThemeToCaptionButtons()
    {
        var frame = App.AppTitlebar as FrameworkElement;
        if (frame != null)
        {
            UpdateTitleBar(frame.ActualTheme);
        }
    }

    public static void UpdateTitleBar(WindowEx window, ElementTheme theme)
    {
        if (window.ExtendsContentIntoTitleBar)
        {
            if (theme != ElementTheme.Default)
            {
                Application.Current.Resources["WindowCaptionForeground"] = theme switch
                {
                    ElementTheme.Dark => new SolidColorBrush(Microsoft.UI.Colors.White),
                    ElementTheme.Light => new SolidColorBrush(Microsoft.UI.Colors.Black),
                    _ => new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                };

                Application.Current.Resources["WindowCaptionForegroundDisabled"] = theme switch
                {
                    ElementTheme.Dark => new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),
                    ElementTheme.Light => new SolidColorBrush(Color.FromArgb(0x66, 0x00, 0x00, 0x00)),
                    _ => new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                };

                Application.Current.Resources["WindowCaptionButtonBackgroundPointerOver"] = theme switch
                {
                    ElementTheme.Dark => new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                    ElementTheme.Light => new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0x00, 0x00)),
                    _ => new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                };

                Application.Current.Resources["WindowCaptionButtonBackgroundPressed"] = theme switch
                {
                    ElementTheme.Dark => new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),
                    ElementTheme.Light => new SolidColorBrush(Color.FromArgb(0x66, 0x00, 0x00, 0x00)),
                    _ => new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                };

                Application.Current.Resources["WindowCaptionButtonStrokePointerOver"] = theme switch
                {
                    ElementTheme.Dark => new SolidColorBrush(Microsoft.UI.Colors.White),
                    ElementTheme.Light => new SolidColorBrush(Microsoft.UI.Colors.Black),
                    _ => new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                };

                Application.Current.Resources["WindowCaptionButtonStrokePressed"] = theme switch
                {
                    ElementTheme.Dark => new SolidColorBrush(Microsoft.UI.Colors.White),
                    ElementTheme.Light => new SolidColorBrush(Microsoft.UI.Colors.Black),
                    _ => new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                };
            }

            Application.Current.Resources["WindowCaptionBackground"] = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            Application.Current.Resources["WindowCaptionBackgroundDisabled"] = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }
    }
}
