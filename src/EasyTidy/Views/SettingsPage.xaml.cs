using H.NotifyIcon.Core;

namespace EasyTidy.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; set; }
    public SettingsPage()
    {
        this.InitializeComponent();
        ViewModel = new SettingsViewModel();
        DataContext = ViewModel;
    }

    private void Click_LanguageRestart(object sender, RoutedEventArgs e)
    {
        ViewModel.Restart();
    }
}

