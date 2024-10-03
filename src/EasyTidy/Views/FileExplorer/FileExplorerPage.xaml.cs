// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class FileExplorerPage : Page
{
    public FileExplorerViewModel ViewModel { get; set;}
    public FileExplorerPage()
    {
        ViewModel = App.GetService<FileExplorerViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.UpdateTaskCommand.Execute((sender as Button).DataContext);
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.DeleteTaskCommand.Execute((sender as Button).DataContext);
    }

    private void RunButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ExecuteTaskCommand.Execute((sender as Button).DataContext);
    }

    private void IsEnableButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.IsEnableTaskCommand.Execute((sender as Button).DataContext);
    }
}
