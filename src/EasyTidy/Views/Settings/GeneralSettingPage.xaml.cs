using EasyTidy.Common.Views;

namespace EasyTidy.Views;

public sealed partial class GeneralSettingPage : ToolPage
{
    public GeneralSettingViewModel ViewModel { get; set; }

    public GeneralSettingPage()
    {
        ViewModel = App.GetService<GeneralSettingViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
        DataContext = ViewModel;
    }

    private void CmbPathType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.SelectPathTypeCommand.Execute(sender);
    }
}


