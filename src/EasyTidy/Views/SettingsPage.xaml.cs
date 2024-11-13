namespace EasyTidy.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; set; }
    public SettingsPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<SettingsViewModel>();
        DataContext = ViewModel;
    }

    private void Click_LanguageRestart(object sender, RoutedEventArgs e)
    {
        ViewModel.Restart();
    }
}

