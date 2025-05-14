// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace EasyTidy.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LogsPage : Page
{
    public LogsViewModel ViewModel { get; }
    public LogsPage()
    {
        ViewModel = App.GetService<LogsViewModel>();
        this.InitializeComponent();
    }

    private void LogsListView_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var scrollViewer = GetFirstDescendantOfType<ScrollViewer>(LogsListView);

        // 只有在需要时才显示内嵌的 Vertical ScrollBar
        if (scrollViewer != null && scrollViewer.VerticalScrollBarVisibility != ScrollBarVisibility.Visible)
        {
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
        }

        // 隐藏外部的滚动条
        if (OuterScrollViewer != null)
            OuterScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
    }

    private void LogsListView_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var scrollViewer = GetFirstDescendantOfType<ScrollViewer>(LogsListView);

        // 恢复内嵌的 Vertical ScrollBar 可见性
        if (scrollViewer != null && scrollViewer.VerticalScrollBarVisibility != ScrollBarVisibility.Hidden)
        {
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        }

        // 恢复外部滚动条的自动显示
        if (OuterScrollViewer != null)
            OuterScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
    }

    private void LogsListView_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        // 获取 ListView 内部的 ScrollViewer
        var scrollViewer = GetFirstDescendantOfType<ScrollViewer>(LogsListView);

        // 只在 scrollViewer 存在的情况下进行处理
        if (scrollViewer != null)
        {
            var properties = e.GetCurrentPoint(LogsListView).Properties;
            double delta = properties.MouseWheelDelta;

            // 通过修改垂直偏移来实现自定义滚动
            scrollViewer.ChangeView(null, scrollViewer.VerticalOffset - delta, null, true);
            e.Handled = true;
        }
    }

    /// 递归查找 ListView 内部的 ScrollViewer
    public static T GetFirstDescendantOfType<T>(DependencyObject root) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T t)
                return t;

            var result = GetFirstDescendantOfType<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }

}
