using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using EasyTidy.Model;

namespace EasyTidy.ViewModels;

public partial class GeneralViewModel : ObservableRecipient
{
    #region 字段、属性

    [ObservableProperty]
    private bool pathTypeSelectedIndex = false;

    [ObservableProperty]
    private bool webDavIsShow = false;

    [ObservableProperty]
    public string floderPath;


    [ObservableProperty]
    private IList<BackupType> backupTypes = Enum.GetValues(typeof(BackupType)).Cast<BackupType>().ToList();

    /// <summary>
    ///     当前配置实例
    /// </summary>
    private ConfigModel? CurConfig = Settings.GeneralConfig;

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

    #endregion


    [RelayCommand]
    private Task OnSelectPathType(object sender)
    {
        var backupType = sender as ComboBox;
        switch (backupType.SelectedItem)
        {
            case BackupType.Local:
                PathTypeSelectedIndex = true;
                WebDavIsShow = false;
                break;
            case BackupType.WebDav:
                PathTypeSelectedIndex = false;
                WebDavIsShow = true;
                break;
            default:
                PathTypeSelectedIndex = false;
                WebDavIsShow = false;
                break;
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OnSelectPath()
    {
        try
        {
            var folder = await FileAndFolderPickerHelper.PickSingleFolderAsync(App.CurrentWindow);
            FloderPath = folder?.Path ?? (WebDavIsShow ? "WebDAV" : "");

        }
        catch (Exception ex)
        {
            FloderPath = "";
            Logger.Error($"ServerViewModel: OnSelectMediaPath 异常信息 {ex}");
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
