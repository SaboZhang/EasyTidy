﻿// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AutomaticPage : Page
{
    public AutomaticViewModel ViewModel { get; set; }
    public AutomaticPage()
    {
        ViewModel = App.GetService<AutomaticViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
    }

    private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.TaskListACV.Refresh();
        ViewModel.SelectedItemChangedCommand.Execute(sender);
    }
}