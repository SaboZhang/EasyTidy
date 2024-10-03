using System.ComponentModel.DataAnnotations;

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

/// <summary>
/// 重名文件处理方式
/// </summary>
public enum FileOperationType
{

    [Display(Name = "跳过")] Skip,

    [Display(Name = "覆盖")] Override,

    [Display(Name = "如果较新则覆盖")] OverwriteIfNewer,

    [Display(Name = "如果大小不同则覆盖")] OverrideIfSizesDiffer,

    [Display(Name = "重命名(01)")] ReNameAppend,

    [Display(Name = "重命名-当前日期")] ReNameAddDate
}

/// <summary>
/// 文件操作模式
/// </summary>
public enum OperationMode
{

    [Display(Name = "移动")] Move,

    [Display(Name = "复制")] Copy,

    [Display(Name = "删除")] Delete,

    [Display(Name = "重命名")] Rename

}
