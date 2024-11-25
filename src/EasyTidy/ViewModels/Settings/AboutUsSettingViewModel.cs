using CommunityToolkit.WinUI;
using EasyTidy.Common.Model;
using EasyTidy.Model;
using System.Collections.ObjectModel;

namespace EasyTidy.ViewModels;
public partial class AboutUsSettingViewModel : ObservableObject
{
    [ObservableProperty]
    public string appInfo = $"{Constants.AppName} v{Constants.Version}";

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public AboutUsSettingViewModel()
    {
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new("Settings".GetLocalized(), typeof(SettingsViewModel).FullName!),
            new("AboutUsSettingPage_Header".GetLocalized(), typeof(AboutUsSettingViewModel).FullName!),
        };
    }
}