namespace EasyTidy.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }
    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        this.InitializeComponent();
        appTitleBar.Window = App.MainWindow;
        ViewModel.JsonNavigationViewService.Initialize(NavView, NavFrame);
        ViewModel.JsonNavigationViewService.ConfigJson("Assets/NavViewMenu/AppData.json");
    }

    private void appTitleBar_BackButtonClick(object sender, RoutedEventArgs e)
    {
        if (NavFrame.CanGoBack)
        {
            NavFrame.GoBack();
        }
    }

    private void appTitleBar_PaneButtonClick(object sender, RoutedEventArgs e)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void NavFrame_Navigated(object sender, NavigationEventArgs e)
    {
        appTitleBar.IsBackButtonVisible = NavFrame.CanGoBack;
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        var element = App.MainWindow.Content as FrameworkElement;

        if (element.ActualTheme == ElementTheme.Light)
        {
            element.RequestedTheme = ElementTheme.Dark;
        }
        else if (element.ActualTheme == ElementTheme.Dark)
        {
            element.RequestedTheme = ElementTheme.Light;
        }
    }

    private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        var viewModel = NavFrame.GetPageViewModel();
        if (viewModel != null && viewModel is ITitleBarAutoSuggestBoxAware titleBarAutoSuggestBoxAware)
        {
            titleBarAutoSuggestBoxAware.OnAutoSuggestBoxTextChanged(sender, args);
        }
    }

    private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var viewModel = NavFrame.GetPageViewModel();
        if (viewModel != null && viewModel is ITitleBarAutoSuggestBoxAware titleBarAutoSuggestBoxAware)
        {
            titleBarAutoSuggestBoxAware.OnAutoSuggestBoxQuerySubmitted(sender, args);
        }
    }

    private void NavViewPaneDisplayModeButton_Click(object sender, RoutedEventArgs e)
    {
        switch (NavView.PaneDisplayMode)
        {
            case NavigationViewPaneDisplayMode.Auto:
                NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                break;
            case NavigationViewPaneDisplayMode.Left:
                NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                break;
            case NavigationViewPaneDisplayMode.Top:
                NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                break;
            case NavigationViewPaneDisplayMode.LeftCompact:
                NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
                break;
            case NavigationViewPaneDisplayMode.LeftMinimal:
                NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Auto;
                break;
        }

        Settings.NavigationViewPaneDisplayMode = NavView.PaneDisplayMode;

    }
}

