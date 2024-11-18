using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Util;

public class CommonUtil
{
    /// <summary>
    ///     执行程序
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static bool ExecuteProgram(string filename, string[] args)
    {
        try
        {
            var arguments = args.Aggregate("", (current, arg) => current + $"\"{arg}\" ");
            arguments = arguments.Trim();
            Process process = new();
            ProcessStartInfo startInfo = new(filename, arguments);
            process.StartInfo = startInfo;
            process.Start();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    ///     是否为管理员权限
    /// </summary>
    /// <returns></returns>
    public static bool IsUserAdministrator()
    {
        var identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);

        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
