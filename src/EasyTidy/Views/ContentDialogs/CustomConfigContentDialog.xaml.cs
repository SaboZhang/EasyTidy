// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views.ContentDialogs;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class CustomConfigContentDialog : ContentDialog
{
    public AutomaticViewModel ViewModel { get; set; }

    public string Delay { get; set; }

    public string Execution { get; set; }

    public string SelectedTime { get; set; } = DateTime.Now.ToString("HH:mm");

    public string MonthlyDay { get; set; }

    public string DayOfMonth { get; set; }

    public string DayOfWeek { get; set; }

    public string Hour { get; set; }

    public string Minute { get; set; }

    public CustomConfigContentDialog()
    {
        ViewModel = App.GetService<AutomaticViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
        RequestedTheme = ViewModel.themeService.GetElementTheme();
    }
}
