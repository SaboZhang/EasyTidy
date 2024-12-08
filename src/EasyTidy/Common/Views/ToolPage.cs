namespace EasyTidy.Common.Views;

public abstract class ToolPage : Page
{
    public ToolPage()
    {
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Focus(FocusState.Programmatic);
    }
}
