using EasyTidy.Model;
using EasyTidy.Util.SettingsInterface;

namespace EasyTidy.Common;

public class SettingsHelper : ISettingsManager
{
    public CommonConfigModel GetConfigModel()
    {
        return new CommonConfigModel
        {
            WebDavPassword = Settings.WebDavPassword,
            WebDavUser = Settings.WebDavUser,
            WebDavUrl = Settings.WebDavUrl,
            SubFolder = Settings.GeneralConfig.SubFolder,
            UploadPrefix = Settings.UploadPrefix
        };
    }
}
