// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views.ContentDialogs;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class PlanExecutionContentDialog : ContentDialog
{
    public IThemeService ThemeService { get; set; }

    public AutomaticViewModel ViewModel { get; set; }

    public PlanExecutionContentDialog()
    {
        ViewModel = App.GetService<AutomaticViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
        RequestedTheme = ViewModel.themeService.GetElementTheme();
    }

}
