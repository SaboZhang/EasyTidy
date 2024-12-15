using System.ComponentModel.DataAnnotations;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Model;
/// <summary>
///     备份方式
/// </summary>
public enum BackupType
{
    [Display(Name = "LocalBackText")]
    Local,

    [Display(Name = "WebDavText")]
    WebDav
}

/// <summary>
/// 重名文件处理方式
/// </summary>
public enum FileOperationType
{

    [Display(Name = "SkipText")] Skip,

    [Display(Name = "OverrideText")] Override,

    [Display(Name = "OverwriteIfNewerText")] OverwriteIfNewer,

    [Display(Name = "OverrideIfSizesDifferText")] OverrideIfSizesDiffer,

    [Display(Name = "ReNameAppendText")] ReNameAppend,

    [Display(Name = "ReNameAddDateText")] ReNameAddDate
}

/// <summary>
/// 文件操作模式
/// </summary>
public enum OperationMode
{

    [Display(Name = "MoveText")] Move,

    [Display(Name = "CopyText")] Copy,

    [Display(Name = "DeleteText")] Delete,

    [Display(Name = "RenameText")] Rename,

    [Display(Name = "RecycleBinText")] RecycleBin,

    [Display(Name = "ExtractText")] Extract,

    [Display(Name = "CompressText")] ZipFile,

    [Display(Name = "UploadWebDavText")] UploadWebDAV,

    [Display(Name = "EncryptionText")] Encryption,

}

public enum YesOrNo
{
    [Display(Name = "NoStr")] No,

    [Display(Name = "YesStr")] Yes,
}

public enum DateUnit
{
    [Display(Name = "SecondText")] Second,

    [Display(Name = "MinuteText")] Minute,

    [Display(Name = "HourText")] Hour,

    [Display(Name = "DayText")] Day,

    [Display(Name = "MonthText")] Month,

    [Display(Name = "YearText")] Year
}

public enum ComparisonResult
{
    [Display(Name = "GreaterThanText")]
    GreaterThan,

    [Display(Name = "EqualToText")]
    Equal,

    [Display(Name = "LessThanText")]
    LessThan,
}

public enum SizeUnit
{
    [Display(Name = "ByteText")] Byte,

    [Display(Name = "KilobyteText")] Kilobyte,

    [Display(Name = "MegabyteText")] Megabyte,

    [Display(Name = "GigabyteText")] Gigabyte
}

public enum ContentOperatorEnum
{
    [Display(Name = "AtLeastOneWord")] AtLeastOneWord,

    [Display(Name = "AtLeastOneWordCaseSensitive")] AtLeastOneWordCaseSensitive,

    [Display(Name = "AllWordsInAnyOrder")] AllWordsInAnyOrder,

    [Display(Name = "AllWordsInAnyOrderCaseSensitive")] AllWordsInAnyOrderCaseSensitive,

    [Display(Name = "RegularExpression")] RegularExpression,

    [Display(Name = "StringText")] String,

    [Display(Name = "StringCaseSensitive")] StringCaseSensitive
}

public enum TaskRuleType
{

    [Display(Name = "HandlingRulesForCustom")] CustomRule,

    [Display(Name = "HandlingFolderRules")] FolderRule,

    [Display(Name = "HandlingRulesForFiles")] FileRule,

    [Display(Name = "RegardedExpressionText")] ExpressionRules

}

public enum BackdropType
{
    None,
    Mica,
    MicaAlt,
    DesktopAcrylic,
    AcrylicThin,
    AcrylicBase,
    Transparent
}
