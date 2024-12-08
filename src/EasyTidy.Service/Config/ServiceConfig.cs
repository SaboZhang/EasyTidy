using EasyTidy.Model;
using EasyTidy.Util.SettingsInterface;

namespace EasyTidy.Service;

public class ServiceConfig
{
    private readonly ISettingsManager _config;

    public static CommonConfigModel? CurConfig;

    public ServiceConfig(ISettingsManager config)
    {
        _config = config;
    }

    public void SetConfigModel()
    {
        CurConfig = _config.GetConfigModel();
    }
}
