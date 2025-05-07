using EasyTidy.Common.Views;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AiSettingsPage : ToolPage
{
    public AiSettingsViewModel ViewModel { get; set; }

    public AiSettingsPage()
    {
        ViewModel = App.GetService<AiSettingsViewModel>();
        this.InitializeComponent();
        DataContext = ViewModel;
        XamlRoot = App.MainWindow.Content.XamlRoot;
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.UpdateAIServiceClickCommand.Execute(sender as Button);
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.DeleteAIServiceClickCommand.Execute(sender as Button);
    }

    private void CheckBox_Checked(object sender, RoutedEventArgs e)
    {
        ViewModel.ChangeIsEnabledCommand.Execute(sender as CheckBox);
    }

    private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        ViewModel.ChangeIsEnabledCommand.Execute(sender as CheckBox);
    }
}
