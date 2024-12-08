using System;
using System.IO;

namespace EasyTidy.Model;

public static class Constants
{
    // 静态构造函数，确保在任何静态成员被访问之前执行
    static Constants()
    {
        // 确保这些成员优先初始化
#if DEBUG
        AppName = "EasyTidyDev";
#else
        AppName = "EasyTidy";
#endif
        Version = InitializationConstants.GetAppVersion();
        RootDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);
        AppConfigPath = Path.Combine(RootDirectoryPath, "AppConfig.json");
        CommonAppConfigPath = Path.Combine(RootDirectoryPath, "CommonAppConfig.json");
        ExecutePath = AppDomain.CurrentDomain.BaseDirectory;
        PortableCnfPath = $"{ExecutePath}{PortableConfig}";
        IsPortable = Directory.Exists(PortableCnfPath);
        CnfPath = IsPortable ? PortableCnfPath : RootDirectoryPath;
    }

    public static readonly string AppName;
    public static readonly string Version;
    public static readonly string RootDirectoryPath;
    public static readonly string AppConfigPath;
    public static readonly string CommonAppConfigPath;

    public static readonly string LogPathName = "v" + Version;

    public static readonly string LogPath = $"{ExecutePath}logs";

    private const string PortableConfig = "portable_config";

    /// <summary>
    ///     用户软件根目录
    /// </summary>
    /// <remarks>
    ///     <see cref="Environment.CurrentDirectory"/>
    ///     * 使用批处理时获取路径为批处理文件所在目录
    /// </remarks>
    public static readonly string ExecutePath;

    public static readonly string PortableCnfPath;

    /// <summary>
    ///     是否为便携模式
    /// </summary>
    private static readonly bool IsPortable;

    /// <summary>
    ///     用户配置目录
    /// </summary>
    public static readonly string CnfPath;

    /// <summary>
    ///     新版本保存目录路径
    /// </summary>
    public static readonly string SaveDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);

    /// <summary>
    ///     新版本保存名字
    /// </summary>
    public static readonly string SaveName = "update.zip";

}
