using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using EasyTidy.Common.Database;
using EasyTidy.Common.Job;
using EasyTidy.Common.Model;
using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.Views.ContentDialogs;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Windows.Storage.Pickers;

namespace EasyTidy.ViewModels;
public partial class GeneralSettingViewModel : ObservableObject
{
    #region 字段 & 属性

    private AppDbContext _dbContext;

    [ObservableProperty]
    private IThemeSelectorService _themeSelectorService;
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

    [ObservableProperty]
    private string _backupStatus;

    [ObservableProperty]
    private string _backupCreateTime;

    [ObservableProperty]
    private string _backupHostName;

    [ObservableProperty]
    private string _backupFileName;

    [ObservableProperty]
    private int _backupTypeIndex = -1;

    [ObservableProperty]
    private ObservableCollection<string> _backList;

    [ObservableProperty]
    public AdvancedCollectionView _backListACV;

    public string WebListSelectedItem { get; set; }

    public object DavItem { get; set; }

    public GeneralSettingViewModel(IThemeSelectorService themeSelectorService)
    {
        ThemeSelectorService = themeSelectorService;
        _dbContext = App.GetService<AppDbContext>();
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new("Settings".GetLocalized(), typeof(SettingsViewModel).FullName!),
            new("Settings_General_Header".GetLocalized(), typeof(GeneralSettingViewModel).FullName!),
        };
    }

    private bool _enableMultiInstance;

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
    //private bool? _irrelevantFiles;

    //public bool IrrelevantFiles
    //{
    //    get
    //    {
    //        return (bool)(_irrelevantFiles ?? CurConfig.IrrelevantFiles);
    //    }

    //    set
    //    {
    //        if (_irrelevantFiles != value)
    //        {
    //            _irrelevantFiles = value;
    //            CurConfig.IrrelevantFiles = value;
    //            NotifyPropertyChanged();
    //        }
    //    }
    //}

    private bool _automaticRepair;

    public bool AutomaticRepair
    {
        get
        {
            return _automaticRepair = CurConfig.AutomaticRepair;
        }
        set
        {
            if (_automaticRepair != value)
            {
                _automaticRepair = value;
                CurConfig.AutomaticRepair = value;
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

    private bool? _isUseProxy;

    public bool IsUseProxy
    {
        get
        {
            return (bool)(_isUseProxy ?? CurConfig.IsUseProxy);
        }

        set
        {
            if (_isUseProxy != value)
            {
                _isUseProxy = value;
                CurConfig.IsUseProxy = value;
                NotifyPropertyChanged();
            }
        }
    }

    private string _proxyAddress;

    public string ProxyAddress
    {
        get
        {
            return _proxyAddress = CurConfig.ProxyAddress;
        }
        set
        {
            if (_proxyAddress != value)
            {
                _proxyAddress = value;
                CurConfig.ProxyAddress = value;
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

    private bool? _autoBackup;

    public bool AutoBackup
    {
        get
        {
            return _autoBackup ?? Settings.BackupConfig.AutoBackup;
        }

        set
        {
            if (_autoBackup != value)
            {
                _autoBackup = value;
                Settings.BackupConfig.AutoBackup = value;
                NotifyPropertyChanged();
            }
        }
    }

    public bool EnableMultiInstance
    {
        get
        {
            return _enableMultiInstance = Settings.GeneralConfig.EnableMultiInstance;
        }
        set
        {
            if (_enableMultiInstance != value)
            {
                _enableMultiInstance = value;
                Settings.GeneralConfig.EnableMultiInstance = value;
                NotifyPropertyChanged();
            }
        }
    }

    private string _webDavPrefix;

    public string WebDavPrefix
    {
        get 
        {
            return _webDavPrefix = Settings.UploadPrefix ?? string.Empty;
        }
        set
        {
            if (_webDavPrefix != value)
            {
                _webDavPrefix = value;
                Settings.UploadPrefix = value;
                NotifyPropertyChanged();
            }
        }
    }

    private bool _preserveDirectoryStructure;

    public bool PreserveDirectoryStructure
    {
        get
        {
            return _preserveDirectoryStructure = Settings.PreserveDirectoryStructure;
        }
        set
        {
            if (_preserveDirectoryStructure != value)
            {
                _preserveDirectoryStructure = value;
                Settings.PreserveDirectoryStructure = value;
                NotifyPropertyChanged();
            }
        }
    }

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    #endregion

    /// <summary>
    ///     初始化
    /// </summary>
    [RelayCommand]
    private void OnPageLoaded()
    {
        BackupStatus = Settings.BackupConfig.BackupVersion ?? string.Empty;
        BackupFileName = Settings.BackupConfig.BackupFileName ?? string.Empty;
        BackupHostName = Settings.BackupConfig.HostName ?? string.Empty;
        BackupCreateTime = Settings.BackupConfig.CreateTime ?? string.Empty;
        WebDavUrl = Settings.WebDavUrl ?? string.Empty;
        WebDavUserName = Settings.WebDavUser ?? string.Empty;
        WebDavPassWord = Settings.WebDavPassword ?? string.Empty;
        WebDavIsShow = Settings.BackupType == BackupType.WebDav;
        BackupTypeIndex = WebDavIsShow ? 1 : PathTypeSelectedIndex ? 0 : -1;
    }

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

    /// <summary>
    ///     备份
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task BackupConfigsClickAsync()
    {
        if (AutoBackup)
        {
            var param = new Dictionary<string, object> { { "LocalPath", FloderPath },{ "WebDavPath", WebDavUrl } };
            await QuartzHelper.AddSimpleJobOfHourAsync<BackupJob>("Backup", Settings.BackupType.ToString(), 24 * 3, param);
        }

        try
        {
            switch (Settings.BackupType)
            {
                case BackupType.Local:
                    // 本地备份 
                    await ExecuteBackupAsync(LocalBackupAsync);
                    break;
                case BackupType.WebDav:
                    // WebDav备份
                    await ExecuteBackupAsync(WebDavBackupAsync);
                    break;
                default:
                    ShowWarningMessage("BackupSelectionTips");
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"备份失败：{ex.Message}");
            ShowErrorMessage("BackupFailedTips");
        }

    }

    [RelayCommand]
    private async Task RestoreBackupClickAsync()
    {
        try
        {
            switch (Settings.BackupType)
            {
                case BackupType.Local:
                    // 本地备份还原 
                    await ExecuteRestoreAsync(LocalRestoreAsync);
                    break;
                case BackupType.WebDav:
                    // WebDav备份还原
                    await GetWebDavList();
                    break;
                default:
                    ShowWarningMessage("RestoreSelectionTips");
                    break;
            }
        }
        catch (Exception ex) 
        {
            Logger.Error($"还原失败：{ex.Message}");
            ShowErrorMessage("RestoreFailedTips");
        }
    }

    /// <summary>
    /// 获取备份列表
    /// </summary>
    /// <returns></returns>
    private async Task GetWebDavList()
    {
        var dialog = new WebDavListContentDialog
        {
            ViewModel = this,
            Title = "BackupList".GetLocalized(),
            PrimaryButtonText = "OK".GetLocalized(),
            CloseButtonText = "CancelText".GetLocalized()
        };

        dialog.PrimaryButtonClick += RestoreBackupButtonAsync;

        try
        {
            var webDavClient = InitializeWebDavClient();
            var list = await webDavClient.ListItemsAsync(WebDavUrl + "/EasyTidy");
            DavItem = list;
            BackList = new ObservableCollection<string>(list.Where(x => !x.IsCollection).Select(x => x.DisplayName));
            BackListACV = new AdvancedCollectionView(BackList);
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Logger.Error($"WebDav连接异常：{ex.Message}");
            ShowErrorMessage("WebDavConnectFailed");
            await DelayCloseMessageVisible();
            return;
        }
    }

    private async void RestoreBackupButtonAsync(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            var list = DavItem as IEnumerable<WebDavItem>;
            var item = list.FirstOrDefault(x => x.DisplayName == WebListSelectedItem);
            if (item != null)
            {
                var webDavClient = InitializeWebDavClient();
                var restorePath = Path.Combine(Constants.CnfPath, "Restore");
                var path = Path.Combine(restorePath, item.DisplayName);
                await webDavClient.DownloadItemAsync(item, path);
                await RestoreFromZipAsync(path);
                ShowSuccessMessage("RestoreSuccessTips");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"还原失败：{ex.Message}");
            ShowErrorMessage("RestoreFailedTips");
        }
        finally
        {
            await CleanRestoreTempFiles();
            await DelayCloseMessageVisible();
        }
    }

    /// <summary>
    /// 本地恢复
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task LocalRestoreAsync()
    {
        var backFile = await FileAndFolderPickerHelper.PickSingleFileAsync(App.MainWindow, [".zip"]) 
            ?? throw new InvalidOperationException("BackupPathText".GetLocalized());
        await RestoreFromZipAsync(backFile.Path);
    }

    [RelayCommand]
    private void OnWebDavUrlChanged(object data)
    {
        var url = data as ListView;
        WebListSelectedItem = url.SelectedItem.ToString();
    }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = null, bool reDoBackupDryRun = true)
    {

        // Notify UI of property change
        OnPropertyChanged(propertyName);

        Logger.Debug($"GeneralViewModel: NotifyPropertyChanged {propertyName}");

        UpdateCurConfig(this);

    }

    /// <summary>
    ///     本地备份
    /// </summary>
    /// <returns></returns>
    private async Task LocalBackupAsync()
    {
        if (string.IsNullOrEmpty(FloderPath))
            throw new InvalidOperationException("BackupPathText".GetLocalized());
        string zipFilePath = PrepareBackupFile();
        ZipUtil.CompressFile(Constants.CnfPath, zipFilePath);
        Settings.BackupType = BackupType.Local;
        ShowBackInfo(zipFilePath);
        await Task.CompletedTask;
    }

    /// <summary>
    ///     显示备份信息
    /// </summary>
    /// <param name="file"></param>
    private void ShowBackInfo(string file)
    {

        BackupStatus = "v" + Constants.Version;
        BackupCreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        BackupFileName = Path.GetFileName(file);
        BackupHostName = WebDavIsShow ? WebDavUrl + "/EasyTidy" : Environment.MachineName;

        Settings.BackupConfig.BackupVersion = BackupStatus;
        Settings.BackupConfig.CreateTime = BackupCreateTime;
        Settings.BackupConfig.BackupFileName = BackupFileName;
        Settings.BackupConfig.HostName = BackupHostName;
        Settings.Save();
    }

    /// <summary>
    ///     WebDav备份
    /// </summary>
    /// <returns></returns>
    private async Task WebDavBackupAsync()
    {
        FloderPath = Constants.ExecutePath;
        string zipFilePath = PrepareBackupFile();
        var webDavClient = InitializeWebDavClient();
        ZipUtil.CompressFile(Constants.CnfPath, zipFilePath);
        await webDavClient.UploadFileAsync(WebDavUrl + "/EasyTidy", zipFilePath);
        ShowBackInfo(zipFilePath);
        File.Delete(zipFilePath);
    }

    private async Task ExecuteBackupAsync(Func<Task> backupFunc)
    {
        try
        {
            await backupFunc();
            ShowSuccessMessage("BackupSuccessTips");
        }
        catch (Exception ex)
        {
            Logger.Error($"备份异常：{ex.Message}");
            ShowErrorMessage("BackupFailedTips");
        }
        finally
        {
            await DelayCloseMessageVisible();
        }
    }

    private async Task ExecuteRestoreAsync(Func<Task> restoreFunc)
    {
        try
        {
            await restoreFunc();
            ShowSuccessMessage("RestoreSuccessTips");
        }
        catch (Exception ex)
        {
            Logger.Error($"还原异常：{ex.Message}");
            ShowErrorMessage("RestoreFailedTips");
        }
        finally
        {
            await CleanRestoreTempFiles();
            await DelayCloseMessageVisible();
        }
    }

    private WebDavClient InitializeWebDavClient()
    {
        // 解密密码
        if (!string.IsNullOrEmpty(Settings.WebDavPassword))
        {
            WebDavPassWord = CryptoUtil.DesDecrypt(Settings.WebDavPassword);
        }

        // 创建 WebDavClient
        var webDavClient = new WebDavClient(
            Settings.WebDavUrl ??= WebDavUrl,
            Settings.WebDavUser ??= WebDavUserName,
            WebDavPassWord
        );

        // 加密并存储密码
        Settings.WebDavPassword ??= CryptoUtil.DesEncrypt(WebDavPassWord);

        // 设置备份类型
        Settings.BackupType = BackupType.WebDav;

        return webDavClient;
    }

    private async Task RestoreFromZipAsync(string zipPath)
    {
        string restorePath = Path.Combine(Constants.CnfPath, "Restore");

        if (!ZipUtil.DecompressToDirectory(zipPath, restorePath))
            throw new InvalidOperationException("RestoreFailedTips".GetLocalized());

        var source = Path.Combine(Constants.CnfPath, "EasyTidy.db");
        var backup = Path.Combine(Constants.CnfPath, "EasyTidy_back.db");

        FileResolver.CopyAllFiles(restorePath, Constants.CnfPath);
        BackupAndRestore.RestoreDatabase(backup, source);
        await Task.CompletedTask;
    }

    private async Task CleanRestoreTempFiles()
    {
        string restorePath = Path.Combine(Constants.CnfPath, "Restore");
        if (Directory.Exists(restorePath))
        {
            Directory.Delete(restorePath, recursive: true);
        }
        await Task.CompletedTask;
    }

    private string PrepareBackupFile()
    {
        var source = Path.Combine(Constants.CnfPath, "EasyTidy.db");
        var backup = Path.Combine(Constants.CnfPath, "EasyTidy_back.db");
        BackupAndRestore.BackupDatabase(source, backup);
        return Path.Combine(FloderPath, $"EasyTidy_backup_{DateTime.Now:yyyyMMddHHmmss}.zip");
    }

    private void ShowSuccessMessage(string localizedKey)
    {
        SettingsBackupRestoreMessageVisible = true;
        BackupRestoreMessageSeverity = InfoBarSeverity.Success;
        SettingsBackupMessage = localizedKey.GetLocalized();
    }

    private void ShowErrorMessage(string localizedKey)
    {
        SettingsBackupRestoreMessageVisible = true;
        BackupRestoreMessageSeverity = InfoBarSeverity.Error;
        SettingsBackupMessage = localizedKey.GetLocalized();
    }

    private void ShowWarningMessage(string localizedKey)
    {
        SettingsBackupRestoreMessageVisible = true;
        BackupRestoreMessageSeverity = InfoBarSeverity.Warning;
        SettingsBackupMessage = localizedKey.GetLocalized();
    }

    /// <summary>
    /// 延时关闭提示
    /// </summary>
    /// <returns></returns>
    private async Task DelayCloseMessageVisible()
    {
        await Task.Delay(10000);
        SettingsBackupRestoreMessageVisible = false;
    }
}
