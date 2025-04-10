﻿using Microsoft.UI.Xaml;

namespace EasyTidy.Model;

public class ConfigModel
{
    /// <summary>
    ///     开机自启动
    /// </summary>

    public ElementTheme ElementTheme { get; set; }

    public bool? IsStartup { get; set; } = false;

    public bool? SubFolder { get; set; } = false;

    public bool? FileInUse { get; set; } = false;

    public bool? IrrelevantFiles { get; set; } = false;

    public bool? Minimize { get; set; } = false;

    public bool? IsStartupCheck { get; set; } = false;

    public bool? IsUseProxy { get; set; } = false;

    public string ProxyAddress { get; set; } = "https://gh-proxy.com/";

    public bool? EmptyFiles { get; set; } = true;

    public bool? HiddenFiles { get; set; } = false;

    public FileOperationType FileOperationType { get; set; } = FileOperationType.Skip;

    public bool EnableMultiInstance { get; set; } = false;

    public virtual bool AutomaticRepair { get; set; } = false;
}
