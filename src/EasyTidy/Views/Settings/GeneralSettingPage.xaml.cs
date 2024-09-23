namespace EasyTidy.Views;

public sealed partial class GeneralSettingPage : Page
{
    public string BreadCrumbBarItemText { get; set; }

    public GeneralSettingPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        BreadCrumbBarItemText = e.Parameter as string;
    }
}


