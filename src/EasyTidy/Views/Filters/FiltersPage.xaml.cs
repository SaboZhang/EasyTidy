// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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
}
