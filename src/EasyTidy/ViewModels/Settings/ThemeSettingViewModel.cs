using Windows.System;

namespace EasyTidy.ViewModels;
public partial class ThemeSettingViewModel : ObservableObject
{
    public IThemeService ThemeService;
    public ThemeSettingViewModel(IThemeService themeService)
    {
        ThemeService = themeService;
    }
   
}
