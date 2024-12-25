using Nucs.JsonSettings;
using Nucs.JsonSettings.Modulation;
using System;

namespace EasyTidy.Model;
public partial class AppConfig : JsonSettings, IVersionable
{
    [EnforcedVersion("1.1.0.1226")]
    public virtual Version Version { get; set; } = new Version(1, 1, 0, 1226);

    public override string FileName { get; set; } = Constants.AppConfigPath;

    public virtual string LastUpdateCheck { get; set; }

    // Docs: https://github.com/Nucs/JsonSettings

    public virtual ConfigModel? GeneralConfig { get; set; } = new();

    public virtual AutomaticConfigModel? AutomaticConfig { get; set; } = new();

    public virtual string Language { get; set; }

    public virtual BackupType BackupType { get; set; }

    public virtual string WebDavUrl { get; set; }

    public virtual string WebDavUser { get; set; }

    public virtual string WebDavPassword { get; set; }

    public virtual string UploadPrefix { get; set; } = "/EasyTidy_UploadFiles";

    public virtual bool PreserveDirectoryStructure { get; set; } = true;

    public virtual BackupModel? BackupConfig { get; set; } = new();
}
