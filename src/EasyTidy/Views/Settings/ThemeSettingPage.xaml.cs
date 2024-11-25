using EasyTidy.Common.Views;

namespace EasyTidy.Views;

public sealed partial class ThemeSettingPage : ToolPage
{
    public ThemeSettingViewModel ViewModel { get; }

    public ThemeSettingPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<ThemeSettingViewModel>();
        DataContext = ViewModel;
    }
}


