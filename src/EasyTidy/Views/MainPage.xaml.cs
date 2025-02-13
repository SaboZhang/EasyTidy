using WinUIEx;

namespace EasyTidy.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        this.InitializeComponent();
    }

    private void PinBtn_Click(object sender, RoutedEventArgs e)
    {
        var top = App.ChildWindow.GetIsAlwaysOnTop();
        if (top)
        {
            App.ChildWindow.SetIsAlwaysOnTop(false);
        }
        else
        {
            App.ChildWindow.SetIsAlwaysOnTop(true);
        }
    }
}

