using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Model;

public class ConfigModel
{
    /// <summary>
    ///     开机自启动
    /// </summary>
    public bool? IsStartup { get; set; } = false;

    public bool? SubFolder { get; set; } = false;

    public bool? FileInUse { get; set; } = false;

    public bool? IrrelevantFiles { get; set; } = false;

    public bool? Minimize { get; set; } = false;
}
