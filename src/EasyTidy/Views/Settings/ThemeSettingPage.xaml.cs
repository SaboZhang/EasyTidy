namespace EasyTidy.Views;

public sealed partial class ThemeSettingPage : Page
{
    public ThemeSettingViewModel ViewModel { get; }
    public string BreadCrumbBarItemText { get; set; }

    public ThemeSettingPage()
    {
        ViewModel = App.GetService<ThemeSettingViewModel>();
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        BreadCrumbBarItemText = e.Parameter as string;
    }
}


