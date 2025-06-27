using System;

namespace EasyTidy.Model;

public class WebDavModel
{
    public WebDAVBackupModel? Backup { get; set; } = new();
    public WebDAVUploadModel? Upload { get; set; } = new();

}

public class WebDAVBackupModel
{
    public string Url { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string UploadPrefix { get; set; } = "/EasyTidy_BackupFiles";

    public bool IsEnabled { get; set; } = false;

}

public class WebDAVUploadModel
{
    public string Url { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string UploadPrefix { get; set; } = "/EasyTidy_UploadFiles";

    public bool IsEnabled { get; set; } = false;

}

