using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Model;

public class BackupModel
{
    public string BackupVersion { get; set; } = "1";

    public string CreateTime { get; set; }

    public string FileName { get; set; }

    public string HostName { get; set; }

}
