using EasyTidy.Common.Views;

namespace EasyTidy.Views;

public sealed partial class SettingsPage : ToolPage
{
    public SettingsViewModel ViewModel { get; }
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

