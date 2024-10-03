// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views.ContentDialogs;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AddTaskContentDialog : ContentDialog
{
    public FileExplorerViewModel ViewModel { get; set; }

    public string TaskName { get; set; }

    public string TaskRule { get; set; }

    public string TaskSource { get; set; }

    public bool Shortcut { get; set; }

    public string TaskTarget { get; set; }

    public bool EnabledFlag { get; set; } = true;

    public AddTaskContentDialog()
    {
        ViewModel = App.GetService<FileExplorerViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
        RequestedTheme = ViewModel.themeService.GetElementTheme();
    }
}
