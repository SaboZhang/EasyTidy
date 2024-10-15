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

    [Display(Name = "重命名")] Rename,

    [Display(Name = "回收站")] RecycleBin

}

public enum YesOrNo
{
    [Display(Name = "否")] No,

    [Display(Name = "是")] Yes,
}

public enum DateUnit
{
    [Display(Name ="秒")] Second,

    [Display(Name ="分钟")] Minute,

    [Display(Name = "小时")] Hour,

    [Display(Name = "天")] Day,

    [Display(Name = "月")] Month,

    [Display(Name = "年")] Year
}

public enum ComparisonResult
{
    [Display(Name = ">")]
    GreaterThan,

    [Display(Name = "=")]
    Equal,

    [Display(Name = "<")]
    LessThan,
}

public enum SizeUnit
{
    [Display(Name = "字节")] Byte,

    [Display(Name = "KB")] Kilobyte,

    [Display(Name = "MB")] Megabyte,

    [Display(Name = "GB")] Gigabyte
}

public enum ContentOperatorEnum
{
    [Display(Name = "至少一个单词")] AtLeastOneWord,

    [Display(Name = "至少一个单词（区分大小写）")] AtLeastOneWordCaseSensitive,

    [Display(Name = "以随意的顺序的所有单词")] AllWordsInAnyOrder,

    [Display(Name = "以随意的顺序的所有单词（区分大小写）")] AllWordsInAnyOrderCaseSensitive,

    [Display(Name = "正则表达式")] RegularExpression,

    [Display(Name = "字符串")] String,

    [Display(Name = "字符串（区分大小写）")] StringCaseSensitive
}