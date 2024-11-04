namespace EasyTidy.Model;

public class BackupModel
{
    public string BackupVersion { get; set; } = "1";

    public string CreateTime { get; set; }

    public string BackupFileName { get; set; }

    public string HostName { get; set; }

    public bool AutoBackup { get; set; } = false;

}
