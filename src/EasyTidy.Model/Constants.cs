using System;
using System.IO;
using WinUICommunity;

namespace EasyTidy.Model;

public static class Constants
{
    public static readonly string AppName = AssemblyInfoHelper.GetAppInfo().Name;
    public static readonly string RootDirectoryPath = Path.Combine(PathHelper.GetLocalFolderPath(), AppName);
    public static readonly string AppConfigPath = Path.Combine(RootDirectoryPath, "AppConfig.json");
    public static readonly string LogDirectoryPath = Path.Combine(RootDirectoryPath, "Log");
    public static readonly string LogFilePath = Path.Combine(LogDirectoryPath, "Log.txt");

    private const string PortableConfig = "portable_config";

    /// <summary>
    ///     用户软件根目录
    /// </summary>
    /// <remarks>
    ///     <see cref="Environment.CurrentDirectory"/>
    ///     * 使用批处理时获取路径为批处理文件所在目录
    /// </remarks>
    public static readonly string ExecutePath = AppDomain.CurrentDomain.BaseDirectory;

    private static readonly string PortableCnfPath = $"{ExecutePath}{PortableConfig}";
}
