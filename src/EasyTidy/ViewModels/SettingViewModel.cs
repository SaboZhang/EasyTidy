using EasyTidy.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EasyTidy.ViewModels;

public partial class SettingViewModel : ObservableObject
{
    private readonly Setting _setting;
    private readonly SettingsViewModel _settingsViewModel;

    public SettingViewModel(Setting setting, SettingsViewModel settingsViewModel)
    {
        _setting = setting;
        _settingsViewModel = settingsViewModel;
    }

    public string Path => _setting.Path;

    public string Header => _setting.Header;

    public string Description => _setting.Description;

    public string Glyph => _setting.Glyph;

    public bool HasToggleSwitch => _setting.HasToggleSwitch;

    [RelayCommand]
    private void NavigateSettings()
    {
        _settingsViewModel.Navigate(_setting.Path);
    }
}
