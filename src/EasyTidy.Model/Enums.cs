using System.ComponentModel;
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

    [Description("跳过目标文件")][Display(Name = "SkipText")] Skip,

    [Description("覆盖目标文件")][Display(Name = "OverrideText")] Override,

    [Description("如果较新则覆盖目标文件")][Display(Name = "OverwriteIfNewerText")] OverwriteIfNewer,

    [Description("如果大小不同则覆盖目标文件")][Display(Name = "OverrideIfSizesDifferText")] OverrideIfSizesDiffer,

    [Description("重命名为'文件名(1)'的格式")][Display(Name = "ReNameAppendText")] ReNameAppend,

    [Description("原文件名称后拼接日期")][Display(Name = "ReNameAddDateText")] ReNameAddDate
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

    [Display(Name = "HardLinkText")] HardLink,

    [Display(Name = "SoftLinkText")] SoftLink,

    [Display(Name = "FileSnapshotText")] FileSnapshot,

    [Display(Name = "AISummaryText")] AISummary,

    [Display(Name = "AIClassificationText")] AIClassification,

    [Display(Name = "RunExternalProgramsText")] RunExternalPrograms

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

    [Display(Name = "BetweenText")]
    Between,

    [Display(Name = "NotBetweenText")]
    NotBetween
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

public enum DateType
{
    Create,
    Edit,
    Visit
}

public enum Encrypted
{
    [Display(Name = "SevenZipText")] SevenZip,
    [Display(Name = "AESText")] AES256WithPBKDF2DerivedKey
}

public enum ServiceType
{
    [Display(Name = "OpenAITxt")]
    OpenAI,
    [Display(Name = "TongyiTxt")]
    QWen,
    [Display(Name = "OllamaTxt")]
    Ollama,
    [Display(Name = "HuggingFaceTxt")]
    HuggingFace,
    [Display(Name = "OpenAIFormatTxt")]
    OpenAIFormat,
    [Display(Name = "GeminiTxt")]
    Gemini,
    [Display(Name = "DeepSeekTxt")]
    DeepSeek,
    [Display(Name = "ClaudeTxt")]
    Claude,
    [Display(Name = "WenxinTxt")]
    WenXin,
    [Display(Name = "AzureTxt")]
    Azure,
    [Display(Name = "HunyuanTxt")]
    Hunyuan,
    [Display(Name = "VolcengineTxt")]
    Volcengine,
    [Display(Name = "SparkTxt")]
    Spark
}

public enum FileType
{
    Unknown,
    Txt,
    Pdf,
    Doc,
    Docx,
    Xls,
    Xlsx
}

public enum PromptType
{
    BuiltIn,
    Custom
}

public enum PropertyCase
{
    PascalCase,
    CamelCase
}
