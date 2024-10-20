using H.NotifyIcon.Core;

namespace EasyTidy.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }
    public SettingsPage()
    {
        ViewModel = new SettingsViewModel();
        this.InitializeComponent();
        DataContext = ViewModel;
    }

    private void Click_LanguageRestart(object sender, RoutedEventArgs e)
    {
        ViewModel.Restart();
    }
}

