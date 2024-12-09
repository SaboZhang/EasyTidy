using WinUIEx;

namespace EasyTidy.Common.Extensions;

public static class WindowExExtensions
{
    public static void SetRequestedTheme(this WindowEx window, ElementTheme theme)
    {
        if (window.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
            TitleBarHelper.UpdateTitleBar(window, rootElement.ActualTheme);
        }
    }

    /// <summary>
    /// 显示错误消息对话框
    /// </summary>
    /// <param name="window"></param>
    /// <param name="title"></param>
    /// <param name="content"></param>
    /// <param name="buttonText"></param>
    /// <returns></returns>
    public static async Task ShowErrorMessageDialogAsync(this WindowEx window, string title, string content, string buttonText)
    {
        await window.ShowMessageDialogAsync(dialog =>
        {
            dialog.Title = title;
            dialog.Content = new TextBlock()
            {
                Text = content,
                TextWrapping = TextWrapping.WrapWholeWords,
            };
            dialog.PrimaryButtonText = buttonText;
        });
    }

    /// <summary>
    /// 显示消息对话框
    /// </summary>
    /// <param name="window"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public static async Task ShowMessageDialogAsync(this WindowEx window, Action<ContentDialog> action)
    {
        var dialog = new ContentDialog()
        {
            XamlRoot = window.Content.XamlRoot,
        };
        action(dialog);
        await dialog.ShowAsync();
    }
}
