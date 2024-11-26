using EasyTidy.Common.Views;

namespace EasyTidy.Views;

public sealed partial class AppUpdateSettingPage : ToolPage
{
    public AppUpdateSettingViewModel ViewModel { get; }

    public AppUpdateSettingPage()
    {
        ViewModel = App.GetService<AppUpdateSettingViewModel>();
        this.InitializeComponent();
        DataContext = ViewModel;
    }
}


