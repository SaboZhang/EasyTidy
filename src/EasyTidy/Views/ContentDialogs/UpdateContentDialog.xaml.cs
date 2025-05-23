// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views.ContentDialogs;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class UpdateContentDialog : ContentDialog
{
    public AppUpdateSettingViewModel ViewModel { get; set; }

    public UpdateContentDialog()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<AppUpdateSettingViewModel>();
        XamlRoot = App.MainWindow.Content.XamlRoot;
    }
}
