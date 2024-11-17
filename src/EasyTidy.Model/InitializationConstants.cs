using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace EasyTidy.Model;

internal static class InitializationConstants
{
    private static readonly FileVersionInfo _fileVersionInfo;

    static InitializationConstants()
    {
        using var process = Process.GetCurrentProcess();
        _fileVersionInfo = process.MainModule.FileVersionInfo;
    }

    public static string GetAppVersion()
    {
        if (IsPackagedApp())
        {
            var version = Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
        else
        {
            return GetVersion()?.ToString() ?? "1.0.0.0";
        }
    }

    private static bool IsPackagedApp()
    {
        try
        {
            return Package.Current != null;
        }
        catch
        {
            return false; // Package.Current 不存在说明是非打包应用
        }
    }

    public static Version GetVersion()
    {
        return new Version(_fileVersionInfo.FileMajorPart, _fileVersionInfo.FileMinorPart, _fileVersionInfo.FileBuildPart, _fileVersionInfo.FilePrivatePart);
    }
}
