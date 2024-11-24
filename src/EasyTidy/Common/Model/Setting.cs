using EasyTidy.Contracts.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Common.Model;

public class Setting
{
    private bool _isExtensionEnabled;

    public string Path { get; }

    public string UniqueId { get; }

    public string Header { get; }

    public string Description { get; }

    public string Glyph { get; }

    public bool HasToggleSwitch { get; }

    public bool HasSettingsProvider { get; }

    //public bool IsExtensionEnabled
    //{
    //    get => _isExtensionEnabled;

    //    set
    //    {
    //        if (_isExtensionEnabled != value)
    //        {
    //            Task.Run(() =>
    //            {
    //                var localSettingsService = App.GetService<ILocalSettingsService>();
    //                return localSettingsService.SaveSettingAsync(UniqueId + "-ExtensionDisabled", !value);
    //            }).Wait();

    //            _isExtensionEnabled = value;

    //            var extensionService = Application.Current.GetService<IExtensionService>();
    //            if (_isExtensionEnabled)
    //            {
    //                extensionService.EnableExtension(UniqueId);
    //            }
    //            else
    //            {
    //                extensionService.DisableExtension(UniqueId);
    //            }
    //        }
    //    }
    //}

    public Setting(string path, string uniqueId, string header, string description, string glyph, bool hasToggleSwitch, bool hasSettingsProvider)
    {
        Path = path;
        UniqueId = uniqueId;
        Header = header;
        Description = description;
        Glyph = glyph;
        HasToggleSwitch = hasToggleSwitch;
        HasSettingsProvider = hasSettingsProvider;

        //_isExtensionEnabled = GetIsExtensionEnabled();
    }

    //private bool GetIsExtensionEnabled()
    //{
    //    var isDisabled = Task.Run(() =>
    //    {
    //        var localSettingsService = App.GetService<ILocalSettingsService>();
    //        return localSettingsService.ReadSettingAsync<bool>(UniqueId + "-ExtensionDisabled");
    //    }).Result;
    //    return !isDisabled;
    //}
}
