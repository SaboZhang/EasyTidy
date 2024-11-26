using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;

namespace EasyTidy.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    //private readonly ResourceManager _resourceManager;

    //private readonly ResourceContext _resourceContext;

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        //_resourceManager = new ResourceManager();
        //_resourceContext = _resourceManager.CreateResourceContext();
        this.InitializeComponent();
        Logger.Fatal("MainPage Initialized");
    }

    private void OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // AutoSuggestBoxHelper.OnITitleBarAutoSuggestBoxTextChangedEvent(sender, args, NavFrame);
    }

    private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        // AutoSuggestBoxHelper.OnITitleBarAutoSuggestBoxQuerySubmittedEvent(sender, args, NavFrame);
    }
}

