namespace EasyTidy.Views;

public sealed partial class AboutUsSettingPage : Page
{
    public AboutUsSettingViewModel ViewModel { get; }
    public string BreadCrumbBarItemText { get; set; }

    public AboutUsSettingPage()
    {
        ViewModel = App.GetService<AboutUsSettingViewModel>();
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        BreadCrumbBarItemText = e.Parameter as string;
    }
}


