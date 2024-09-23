namespace EasyTidy.ViewModels;
public partial class AboutUsSettingViewModel : ObservableObject
{
    [ObservableProperty]
    public string appInfo = $"{App.Current.AppName} v{App.Current.AppVersion}";
}