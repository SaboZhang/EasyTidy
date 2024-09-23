namespace EasyTidy.Views;

public sealed partial class ThemeSettingPage : Page
{
    public ThemeSettingViewModel ViewModel { get; }
    public string BreadCrumbBarItemText { get; set; }

    public ThemeSettingPage()
    {
        ViewModel = App.GetService<ThemeSettingViewModel>();
        this.InitializeComponent();
        Loaded += ThemeSettingPage_Loaded;
    }

    private void ThemeSettingPage_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.ThemeService.SetThemeComboBoxDefaultItem(CmbTheme);
        ViewModel.ThemeService.SetBackdropComboBoxDefaultItem(CmbBackdrop);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        BreadCrumbBarItemText = e.Parameter as string;
    }
}


