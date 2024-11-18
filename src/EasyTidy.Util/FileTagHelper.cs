using System.IO;
using System.Runtime.InteropServices;

namespace EasyTidy.Util;

public class FileTagHelper
{
    // [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    // private static extern bool SetFileAttributes(string lpFileName, uint dwFileAttributes);

    // public static void AddTag(string filePath, string tag)
    // {
    //     var file = Shell32.Shell.GetFile(filePath);
    //     file.ExtendedProperty["System.Keywords"] = tag; // 添加标签
    //     file.CommitChanges();
    // }
}
