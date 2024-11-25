using EasyTidy.Common.Views;

namespace EasyTidy.Views;

public sealed partial class AboutUsSettingPage : ToolPage
{
    public AboutUsSettingViewModel ViewModel { get; }

    public AboutUsSettingPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<AboutUsSettingViewModel>();
        DataContext = ViewModel;
    }
}


