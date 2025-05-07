// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views.ContentDialogs;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class WebDavListContentDialog : ContentDialog
{
    public GeneralSettingViewModel ViewModel { get; set; }
    public WebDavListContentDialog()
    {
        ViewModel = App.GetService<GeneralSettingViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
        RequestedTheme = ViewModel.ThemeSelectorService.Theme;
    }

    private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.WebDavUrlChangedCommand.Execute(sender);
    }
}
