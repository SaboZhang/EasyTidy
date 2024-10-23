using System;
using System.IO;
using WinUICommunity;

namespace EasyTidy.Model;

public static class Constants
{
    public static readonly string AppName = AssemblyInfoHelper.GetAppInfo().Name;
    public static readonly string RootDirectoryPath = Path.Combine(PathHelper.GetLocalFolderPath(), AppName);
    public static readonly string AppConfigPath = Path.Combine(RootDirectoryPath, "AppConfig.json");
    public static readonly string CommonAppConfigPath = Path.Combine(RootDirectoryPath, "CommonAppConfig.json");

    public static readonly string LogPathName = AssemblyInfoHelper.GetAppInfo().Version.ToString();

    private const string PortableConfig = "portable_config";

    /// <summary>
    ///     用户软件根目录
    /// </summary>
    /// <remarks>
    ///     <see cref="Environment.CurrentDirectory"/>
    ///     * 使用批处理时获取路径为批处理文件所在目录
    /// </remarks>
    public static readonly string ExecutePath = AppDomain.CurrentDomain.BaseDirectory;

    public static readonly string PortableCnfPath = $"{ExecutePath}{PortableConfig}";

    /// <summary>
    ///     是否为便携模式
    /// </summary>
    private static readonly bool IsPortable = Directory.Exists(PortableCnfPath);

    /// <summary>
    ///     用户配置目录
    /// </summary>
    public static readonly string CnfPath = IsPortable
            ? PortableCnfPath
            : RootDirectoryPath;

}
