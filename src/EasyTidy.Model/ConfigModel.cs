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

    public bool? IsStartupCheck { get; set; } = false;

    public bool? EmptyFiles { get; set; } = true;

    public bool? HiddenFiles { get; set; } = false;

    public FileOperationType FileOperationType { get; set; } = FileOperationType.Skip;

    public bool EnableMultiInstance { get; set; } = false;
}
