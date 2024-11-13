using Windows.System;

namespace EasyTidy.ViewModels;
public partial class ThemeSettingViewModel : ObservableObject
{
    public IThemeService ThemeService;
    public ThemeSettingViewModel(IThemeService themeService)
    {
        ThemeService = themeService;
    }

    [RelayCommand]
    private void OnBackdropChanged(object sender)
    {
        ThemeService.OnBackdropComboBoxSelectionChanged(sender);
    }

    [RelayCommand]
    private void OnThemeChanged(object sender)
    {
        ThemeService.OnThemeComboBoxSelectionChanged(sender);
    }

    [RelayCommand]
    private async Task OpenWindowsColorSettings()
    {
        _ = await Launcher.LaunchUriAsync(new Uri("ms-settings:colors"));
    }
}
