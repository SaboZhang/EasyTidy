// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace EasyTidy.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class FiltersPage : Page
{
    public FilterViewModel ViewModel { get; set; }

    /// <summary>
    /// Initializes a new instance of the FiltersPage class.
    /// </summary>
    public FiltersPage()
    {
        ViewModel = App.GetService<FilterViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.UpdateFilterCommand.Execute((sender as Button).DataContext);
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.DeleteFilterCommand.Execute((sender as Button).DataContext);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.Initialize(NotificationQueue);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.Uninitialize();
    }

    private void LogsListView_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var scrollViewer = GetFirstDescendantOfType<ScrollViewer>(FiltersListView);
        if (scrollViewer != null)
        {
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
        }
        if (OuterScrollViewer != null)
            OuterScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
    }

    private void LogsListView_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var scrollViewer = GetFirstDescendantOfType<ScrollViewer>(FiltersListView);
        if (scrollViewer != null)
        {
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        }
        if (OuterScrollViewer != null)
            OuterScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
    }

    private void LogsListView_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var scrollViewer = GetFirstDescendantOfType<ScrollViewer>(FiltersListView);
        if (scrollViewer != null)
        {
            var properties = e.GetCurrentPoint(FiltersListView).Properties;
            double delta = properties.MouseWheelDelta;

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
