using EasyTidy.Model;
using Nucs.JsonSettings;
using Nucs.JsonSettings.Modulation;

namespace EasyTidy.Common;
public class AppConfig : JsonSettings, IVersionable
{
    [EnforcedVersion("1.0.0.0")]
    public virtual Version Version { get; set; } = new Version(1, 0, 0, 0);

    public override string FileName { get; set; } = Constants.AppConfigPath;

    public virtual string LastUpdateCheck { get; set; }

    public virtual NavigationViewPaneDisplayMode NavigationViewPaneDisplayMode { get; set; } = NavigationViewPaneDisplayMode.Left;

    // Docs: https://github.com/Nucs/JsonSettings

    public virtual ConfigModel? GeneralConfig { get; set; } = InitialConfig();

}
