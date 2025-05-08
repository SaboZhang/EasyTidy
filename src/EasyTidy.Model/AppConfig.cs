using Nucs.JsonSettings;
using Nucs.JsonSettings.Modulation;
using System;

namespace EasyTidy.Model;
public partial class AppConfig : JsonSettings, IVersionable
{
    [EnforcedVersion("1.2.4.305")]
    public virtual Version Version { get; set; } = new Version(1, 2, 4, 305);

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

    public virtual Encrypted Encrypted { get; set; }

    public virtual string EncryptedPassword { get; set; }

    public virtual bool OriginalFile { get; set; } = false;

    public virtual BackupModel? BackupConfig { get; set; } = new();

    public virtual bool IdOrder { get; set; } = false;

    public virtual bool EnabledRightClick { get; set; } = true;
    
}
