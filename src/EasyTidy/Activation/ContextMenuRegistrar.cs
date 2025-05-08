using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace EasyTidy.Activation;

public class ContextMenuRegistrar
{
    public static void TryRegister()
    {
        string exePath = Process.GetCurrentProcess().MainModule.FileName;

        if (!IsRegistered(exePath))
        {
            Register(exePath);
        }
    }

    private static bool IsRegistered(string exePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\*\shell\EasyTidy\command");
        var value = key?.GetValue("") as string;
        return string.Equals(value, $"\"{exePath}\" \"%1\"", StringComparison.OrdinalIgnoreCase);
    }

    private static void Register(string exePath)
    {
        const string menuName = "EasyTidy";
        const string menuText = "用 EasyTidy 处理";

        var fileKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\*\shell\{menuName}");
        fileKey.SetValue("", menuText);
        fileKey.SetValue("Icon", $"\"{exePath}\"");
        fileKey.CreateSubKey("command").SetValue("", $"\"{exePath}\" \"%1\"");

        var folderKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\Directory\shell\{menuName}");
        folderKey.SetValue("", menuText);
        folderKey.SetValue("Icon", $"\"{exePath}\"");
        folderKey.CreateSubKey("command").SetValue("", $"\"{exePath}\" \"%1\"");
    }

    public static void Unregister()
    {
        const string menuName = "EasyTidy";

        try
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\*\shell\{menuName}", throwOnMissingSubKey: false);
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\Directory\shell\{menuName}", throwOnMissingSubKey: false);
        }
        catch (Exception ex)
        {
            // 日志或提示
            Console.WriteLine($"卸载右键菜单失败：{ex.Message}");
        }
    }
    
}
