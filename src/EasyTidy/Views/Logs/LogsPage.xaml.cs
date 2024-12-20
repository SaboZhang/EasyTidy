// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LogsPage : Page
{
    public LogsViewModel ViewModel { get; }
    public LogsPage()
    {
        ViewModel = App.GetService<LogsViewModel>();
        this.InitializeComponent();
    }
}
