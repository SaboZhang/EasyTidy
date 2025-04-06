using CommunityToolkit.WinUI;
using EasyTidy.Common.Model;
using EasyTidy.Model;
using EasyTidy.Service.AIService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.ViewModels;

public class AiSettingsViewModel : ObservableObject
{
    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public AiSettingsViewModel()
    {
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new("Settings".GetLocalized(), typeof(SettingsViewModel).FullName!),
            new("AiSettingPage_Header".GetLocalized(), typeof(AiSettingsViewModel).FullName!),
        };
    }
}
