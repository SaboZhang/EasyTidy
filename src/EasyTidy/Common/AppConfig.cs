using EasyTidy.Model;
using Nucs.JsonSettings;
using Nucs.JsonSettings.Modulation;

namespace EasyTidy.Common;
public partial class AppConfig : JsonSettings, IVersionable
{
    [EnforcedVersion("1.0.3.1120")]
    public virtual Version Version { get; set; } = new Version(1, 0, 3, 1120);

    public override string FileName { get; set; } = Constants.AppConfigPath;

    public virtual string LastUpdateCheck { get; set; }

    public virtual NavigationViewPaneDisplayMode NavigationViewPaneDisplayMode { get; set; } = NavigationViewPaneDisplayMode.Auto;

    // Docs: https://github.com/Nucs/JsonSettings

    public virtual ConfigModel? GeneralConfig { get; set; } = InitialConfig();

    public virtual AutomaticConfigModel? AutomaticConfig { get; set; } = new();

    public virtual string Language { get; set; }

    public virtual BackupType BackupType { get; set; }

    public virtual string WebDavUrl { get; set; }

    public virtual string WebDavUser { get; set; }

    public virtual string WebDavPassword { get; set; }

    public virtual BackupModel? BackupConfig { get; set; } = new();
}
