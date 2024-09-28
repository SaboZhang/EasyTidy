// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class GeneralPage : Page
{

    public GeneralViewModel ViewModel { get; set; }
    public GeneralPage()
    {
        ViewModel = App.GetService<GeneralViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
        //Loaded += General_Loaded;
    }

    //private void General_Loaded(object sender, RoutedEventArgs e)
    //{
    //    CmbPathType.SelectedIndex = 0;
    //}

    private void CmbPathType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.SelectPathTypeCommand.Execute(sender);
    }
}
