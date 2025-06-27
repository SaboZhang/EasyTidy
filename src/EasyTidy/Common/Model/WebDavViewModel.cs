using System;

namespace EasyTidy.Common.Model;

// 用于绑定上传设置的 ViewModel
public partial class WebDavUploadViewModel : ObservableObject
{
    [ObservableProperty] private bool enabled;
    [ObservableProperty] private string serverUrl = string.Empty;
    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string uploadPath = "/";
}

// 用于绑定备份设置的 ViewModel
public partial class WebDavBackupViewModel : ObservableObject
{
    [ObservableProperty] private bool enabled;
    [ObservableProperty] private string serverUrl = string.Empty;
    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string backupPath = "/";
}

