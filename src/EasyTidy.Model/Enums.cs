using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Model;
/// <summary>
///     备份方式
/// </summary>
public enum BackupType
{
    [Display(Name = "本地备份")]
    Local,

    [Display(Name = "WebDav")]
    WebDav
}

public enum FileOperationType
{

    [Display(Name = "跳过")] Skip,

    [Display(Name = "覆盖")] Override,

    [Display(Name = "如果较新则覆盖")] OverwriteIfNewer,

    [Display(Name = "如果大小不同则覆盖")] OverrideIfSizesDiffer,

    [Display(Name = "重命名(01)")] ReNameAppend,

    [Display(Name = "重命名-当前日期")] ReNameAddDate
}
