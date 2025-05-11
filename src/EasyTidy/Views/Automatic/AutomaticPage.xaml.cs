// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


using EasyTidy.Common.Model;

namespace EasyTidy.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AutomaticPage : Page
{
    public AutomaticViewModel ViewModel { get; set; }
    public AutomaticPage()
    {
        ViewModel = App.GetService<AutomaticViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;

    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.Initialize(NotificationQueue);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.Uninitialize();
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CustomConfigCommand.Execute((sender as Button).DataContext);
    }

    private void ViewButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.PreviewTaskCommand.Execute((sender as Button).DataContext);
    }

    private void Checkbox_Click(object sender, RoutedEventArgs e)
    {
        var checkBox = sender as CheckBox;
        var item = checkBox.DataContext as TaskItem;
        ViewModel.UpdateCheckedCommand.Execute(item);
    }

    private void ToggleView_Click(object sender, RoutedEventArgs e)
    {
        if (TaskItemsView.Visibility == Visibility.Visible)
        {
            TaskItemsView.Visibility = Visibility.Collapsed;
            TaskListViews.Visibility = Visibility.Visible;
        }
        else
        {
            TaskItemsView.Visibility = Visibility.Visible;
            TaskListViews.Visibility = Visibility.Collapsed;
        }
    }
}
