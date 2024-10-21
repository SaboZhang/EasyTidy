using CommunityToolkit.WinUI;
using EasyTidy.Model;
using EasyTidy.Util;
using System.Runtime.CompilerServices;

namespace EasyTidy.ViewModels;

public partial class GeneralViewModel : ObservableRecipient
{
    #region 字段 & 属性

    /// <summary>
    ///     当前配置实例
    /// </summary>
    private static ConfigModel? CurConfig = Settings.GeneralConfig;

    [ObservableProperty]
    private bool pathTypeSelectedIndex = false;

    [ObservableProperty]
    private bool webDavIsShow = false;

    [ObservableProperty]
    public string floderPath;

    /// <summary>
    ///     文件冲突操作方式
    /// </summary>
    [ObservableProperty]
    private IList<FileOperationType> fileOperationTypes = Enum.GetValues(typeof(FileOperationType)).Cast<FileOperationType>().ToList();

    [ObservableProperty]
    private FileOperationType _operationType = CurConfig.FileOperationType;

    /// <summary>
    ///     备份类型
    /// </summary>
    [ObservableProperty]
    private IList<BackupType> backupTypes = Enum.GetValues(typeof(BackupType)).Cast<BackupType>().ToList();

    [ObservableProperty]
    private string _settingsBackupMessage;

    [ObservableProperty]
    private InfoBarSeverity _backupRestoreMessageSeverity;

    [ObservableProperty]
    private bool _settingsBackupRestoreMessageVisible;

    [ObservableProperty]
    private string _webDavUserName;

    [ObservableProperty]
    private string _webDavPassWord;

    [ObservableProperty]
    private string _webDavUrl;

    /// <summary>
    /// 是否处理子文件夹
    /// </summary>
    private bool? _subFolder;

    public bool SubFolder
    {
        get
        {
            return (bool)(_subFolder ?? CurConfig.SubFolder);
        }

        set
        {
            if (_subFolder != value)
            {
                _subFolder = value;
                CurConfig.SubFolder = value;
                NotifyPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否处理使用中的文件
    /// </summary>
    private bool? _fileInUse;

    public bool FileInUse
    {
        get
        {
            return (bool)(_fileInUse ?? CurConfig.FileInUse);
        }

        set
        {
            if (_fileInUse != value)
            {
                _fileInUse = value;
                CurConfig.FileInUse = value;
                NotifyPropertyChanged();
            }
        }
    }

    /// <summary>
    ///     是否最小化到托盘
    /// </summary>
    private bool? _minimize;

    public bool Minimize
    {
        get
        {
            return (bool)(_minimize ?? CurConfig.Minimize);
        }

        set
        {
            if (_minimize != value)
            {
                _minimize = value;
                CurConfig.Minimize = value;
                NotifyPropertyChanged();
            }
        }
    }

    /// <summary>
    ///     是否处理无关文件
    /// </summary>
    private bool? _irrelevantFiles;

    public bool IrrelevantFiles
    {
        get
        {
            return (bool)(_irrelevantFiles ?? CurConfig.IrrelevantFiles);
        }

        set
        {
            if (_irrelevantFiles != value)
            {
                _irrelevantFiles = value;
                CurConfig.IrrelevantFiles = value;
                NotifyPropertyChanged();
            }
        }
    }

    private bool? _isStartupCheck;

    public bool IsStartupCheck
    {
        get
        {
            return (bool)(_isStartupCheck ?? CurConfig.IsStartupCheck);
        }

        set
        {
            if (_isStartupCheck != value)
            {
                _isStartupCheck = value;
                CurConfig.IsStartupCheck = value;
                NotifyPropertyChanged();
            }
        }
    }
    /// <summary>
    ///     是否开机启动
    /// </summary>
    private bool? _isStartup;

    public bool IsStartup
    {
        get
        {
            return (bool)(_isStartup ?? CurConfig.IsStartup);
        }

        set
        {
            if (_isStartup != value)
            {
                _isStartup = value;
                CurConfig.IsStartup = value;
                NotifyPropertyChanged();
            }
        }
    }

    /// <summary>
    ///     是否处理空文件或者空文件夹
    /// </summary>
    private bool? _emptyFiles;

    public bool EmptyFiles
    {
        get
        {
            return (bool)(_emptyFiles ?? CurConfig.EmptyFiles);
        }

        set
        {
            if (_emptyFiles != value)
            {
                _emptyFiles = value;
                CurConfig.EmptyFiles = value;
                NotifyPropertyChanged();
            }
        }
    }

    /// <summary>
    ///     是否处理隐藏文件
    /// </summary>
    private bool? _isHiddenFiles;

    public bool HiddenFiles
    {
        get
        {
            return (bool)(_isHiddenFiles ?? CurConfig.HiddenFiles);
        }

        set
        {
            if (_isHiddenFiles != value)
            {
                _isHiddenFiles = value;
                CurConfig.HiddenFiles = value;
                NotifyPropertyChanged();
            }
        }
    }

    #endregion

    [RelayCommand]
    private void OnFileOperationTypeChanged(object sender)
    {
        UpdateCurConfig(this);
    }

    [RelayCommand]
    private void OnSelectPathType(object sender)
    {
        var backupType = sender as ComboBox;
        switch (backupType.SelectedItem)
        {
            case BackupType.Local:
                PathTypeSelectedIndex = true;
                WebDavIsShow = false;
                Settings.BackupType = BackupType.Local;
                break;
            case BackupType.WebDav:
                PathTypeSelectedIndex = false;
                WebDavIsShow = true;
                Settings.BackupType = BackupType.WebDav;
                break;
            default:
                PathTypeSelectedIndex = false;
                WebDavIsShow = false;
                break;
        }
    }

    [RelayCommand]
    private async Task OnSelectPath()
    {
        try
        {
            var folder = await FileAndFolderPickerHelper.PickSingleFolderAsync(App.MainWindow);
            FloderPath = folder?.Path ?? (WebDavIsShow ? "WebDAV" : "");

        }
        catch (Exception ex)
        {
            FloderPath = "";
            Logger.Error($"ServerViewModel: OnSelectMediaPath 异常信息 {ex}");
        }
    }

    [RelayCommand]
    private async Task BackupConfigsClickAsync()
    {
        string backupAndRestoreDir = Constants.PortableCnfPath ?? Constants.RootDirectoryPath;

        switch (Settings.BackupType)
        {
            case BackupType.Local:
                // 本地备份
                BackupRestoreMessageSeverity = InfoBarSeverity.Success;
                SettingsBackupMessage = "BackupSuccessTips".GetLocalized();
                break;
            case BackupType.WebDav:
                // WebDav备份
                WebDavClient webDavClient = new(WebDavUrl, WebDavUserName, WebDavPassWord);
                var backup = await webDavClient.UploadFileAsync("", "");
                if (backup)
                {
                    BackupRestoreMessageSeverity = InfoBarSeverity.Success;
                    SettingsBackupMessage = "BackupSuccessTips".GetLocalized();
                }
                else
                {
                    BackupRestoreMessageSeverity = InfoBarSeverity.Error;
                    SettingsBackupMessage = "BackupFailedTips".GetLocalized();
                }
                break;
            default:
                BackupRestoreMessageSeverity = InfoBarSeverity.Warning;
                SettingsBackupMessage = "BackupSelectionTips".GetLocalized();
                break;
        }
        
    }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = null, bool reDoBackupDryRun = true)
    {

        // Notify UI of property change
        OnPropertyChanged(propertyName);

        Logger.Info($"GeneralViewModel: NotifyPropertyChanged {propertyName}");

        UpdateCurConfig(this);

    }
}
