using System;
using EasyTidy.Model;
using EasyTidy.Util.SettingsInterface;

namespace EasyTidy.Common;

public class SettingsHelper : ISettingsManager
{
    public ConfigModel GetConfigModel()
    {
        return new ConfigModel
        {
            Minimize = Settings?.GeneralConfig.Minimize ?? false,
            IrrelevantFiles = Settings?.GeneralConfig.IrrelevantFiles ?? false,
            FileInUse = Settings?.GeneralConfig.FileInUse ?? false,
            SubFolder = Settings?.GeneralConfig.SubFolder ?? false,
            IsStartup = Settings?.GeneralConfig.IsStartup ?? false,
            IsStartupCheck = Settings?.GeneralConfig.IsStartupCheck?? false,
        };
    }
}
