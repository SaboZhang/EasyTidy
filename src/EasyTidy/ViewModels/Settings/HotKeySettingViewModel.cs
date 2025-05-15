using CommunityToolkit.WinUI;
using EasyTidy.Common.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.ViewModels;

public partial class HotKeySettingViewModel : ObservableObject
{
    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public HotKeySettingViewModel()
    {
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new("Settings".GetLocalized(), typeof(SettingsViewModel).FullName!),
            new("Settings_Hotkey_Header".GetLocalized(), typeof(HotKeySettingViewModel).FullName!),
        };
    }
}
