﻿using CommunityToolkit.WinUI;
using EasyTidy.Common.Model;
using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using System.Collections.ObjectModel;

namespace EasyTidy.ViewModels;
public partial class AppUpdateSettingViewModel : ObservableObject
{
    private readonly IThemeSelectorService _themeSelectorService;

    [ObservableProperty]
    public string currentVersion;

    [ObservableProperty]
    public string lastUpdateCheck;

    [ObservableProperty]
    public bool isUpdateAvailable;

    [ObservableProperty]
    public bool isLoading;

    [ObservableProperty]
    public bool isCheckButtonEnabled = true;

    [ObservableProperty]
    public string loadingStatus = "Status".GetLocalized();

    private string ChangeLog = string.Empty;

    private string DownloadUrl = string.Empty;

    private string NewVersion = string.Empty;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public AppUpdateSettingViewModel(IThemeSelectorService themeSelectorService)
    {
        _themeSelectorService = themeSelectorService;

        CurrentVersion = string.Format("CurrentVersion".GetLocalized(), Constants.Version);
        LastUpdateCheck = Settings.LastUpdateCheck;

        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new("Settings".GetLocalized(), typeof(SettingsViewModel).FullName!),
            new("AppUpdateSetting_Header".GetLocalized(), typeof(AppUpdateSettingViewModel).FullName!),
        };
    }

    /// <summary>
    /// 检查更新
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task CheckForUpdateAsync()
    {
        IsLoading = true;
        IsUpdateAvailable = false;
        IsCheckButtonEnabled = false;
        LoadingStatus = "CheckingForNewVersion".GetLocalized();
        if (CommonUtil.IsNetworkAvailable())
        {
            try
            {
                string username = "SaboZhang";
                string repo = "EasyTidy";
                LastUpdateCheck = DateTime.Now.ToShortDateString();
                Settings.LastUpdateCheck = DateTime.Now.ToShortDateString();
                var update = await UpdateHelper.CheckUpdateAsync(username, repo, new Version(App.Current.AppVersion));
                if (update.IsExistNewVersion)
                {
                    NewVersion = update.TagName;
                    IsUpdateAvailable = true;
                    ChangeLog = update.Changelog;
                    DownloadUrl = update.Assets.FirstOrDefault(a => a.Url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))?.Url ?? "";
                    LoadingStatus = string.Format("FoundANewVersion".GetLocalized(), update.TagName, update.CreatedAt, update.PublishedAt);
                }
                else
                {
                    LoadingStatus = "LatestVersion".GetLocalized();
                }
            }
            catch (Exception ex)
            {
                LoadingStatus = ex.Message;
                IsLoading = false;
                IsCheckButtonEnabled = true;
            }
        }
        else
        {
            LoadingStatus = "ErrorConnection".GetLocalized();
        }
        IsLoading = false;
        IsCheckButtonEnabled = true;
    }

    /// <summary>
    /// 启动时检查更新
    /// </summary>
    /// <returns></returns>
    public async Task CheckForNewVersionAsync()
    {
        if (CommonUtil.IsNetworkAvailable() && (bool)Settings.GeneralConfig.IsStartupCheck)
        {
            try
            {
                string username = "SaboZhang";
                string repo = "EasyTidy";
                LastUpdateCheck = DateTime.Now.ToShortDateString();
                Settings.LastUpdateCheck = DateTime.Now.ToShortDateString();
                var update = await UpdateHelper.CheckUpdateAsync(username, repo, new Version(App.Current.AppVersion));
                if (update.IsExistNewVersion)
                {
                    NewVersion = update.TagName;
                    IsUpdateAvailable = true;
                    ChangeLog = update.Changelog;
                    LoadingStatus = string.Format("FoundANewVersion".GetLocalized(), update.TagName, update.CreatedAt, update.PublishedAt);
                    App.MainWindow.DispatcherQueue.TryEnqueue(async () =>
                    {
                        await ShowUpdateDialogAsync(update.Changelog,
                            update.Assets.FirstOrDefault(a => a.Url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))?.Url ?? "");
                    });
                }
                else
                {
                    Logger.Info($"已是新版本");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Checking for update failed：{ex.Message}");
            }
        }
        else
        {
            Logger.Warn($"Checking for updates failed：Automatic startup update is not enabled or the network is abnormal.");
        }
    }

    private async Task ShowUpdateDialogAsync(string changelog, string downloadUrl)
    {
        var mainWindow = App.MainWindow;
        if (mainWindow == null)
        {
            Logger.Error("Main window is not initialized.");
            throw new InvalidOperationException("Main window is not initialized.");
        }

        var isShow = false;

        // 创建进度条控件
        var progressBar = new ProgressBar
        {
            IsIndeterminate = false,
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var progressText = new TextBlock
        {
            Text = "PreparingToDownload".GetLocalized(),
            Margin = new Thickness(0, 5, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left
        };

        // 使用 Grid 来确保 UI 在小屏幕时不溢出
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 可伸缩的内容区域
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 固定进度条区域
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 固定文本区域

        // 更新日志区域（可滚动）
        var scrollViewer = new ScrollViewer
        {
            Content = new TextBlock
            {
                Text = changelog.Replace("-", "•"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10)
            },
            Margin = new Thickness(10),
            MaxHeight = 480, // 限制高度，防止遮挡下面的进度条
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        Grid.SetRow(scrollViewer, 0);
        grid.Children.Add(scrollViewer);

        // 进度文本
        Grid.SetRow(progressText, 1);
        grid.Children.Add(progressText);

        // 进度条
        Grid.SetRow(progressBar, 2);
        grid.Children.Add(progressBar);

        // 创建 ContentDialog 来显示更新信息
        var updateDialog = new ContentDialog
        {
            Title = "ReleaseNotes".GetLocalized(),
            Content = grid,
            CloseButtonText = "Close".GetLocalized(),
            PrimaryButtonText = "Download".GetLocalized(),
            XamlRoot = mainWindow.Content.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
            RequestedTheme = _themeSelectorService.Theme
        };

        // 下载按钮的点击事件
        updateDialog.PrimaryButtonClick += async (sender, args) =>
        {
            var deferral = args.GetDeferral();
            try
            {
                var progressReporter = new Progress<double>(progress =>
                {
                    progressBar.Value = progress;
                    progressText.Text = "Downloading".GetLocalized() + $"... {progress:F1}%";
                });

                await Download(downloadUrl, progressReporter);
                progressText.Text = "DownloadComplete".GetLocalized();
                isShow = true;
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                isShow = false;
                await new ContentDialog
                {
                    Title = "Error",
                    Content = $"Could not open download link. {ex.Message}",
                    CloseButtonText = "OK".GetLocalized(),
                    XamlRoot = App.MainWindow.Content.XamlRoot
                }.ShowAsync();
            }
            finally
            {
                deferral.Complete();
                if (isShow)
                {
                    await new ContentDialog
                    {
                        Title = "UpdateReady".GetLocalized(),
                        Content = "Update_Ready_Content".GetLocalized(),
                        CloseButtonText = "OK".GetLocalized(),
                        XamlRoot = App.MainWindow.Content.XamlRoot,
                        RequestedTheme = _themeSelectorService.Theme
                    }.ShowAsync();
                    InstallUpdate();
                }
            }
        };

        // 显示对话框
        await updateDialog.ShowAsync();
    }

    private async Task Download(string downloadUrl, IProgress<double>? progress = null)
    {
        var httpClient = new HttpClient(new SocketsHttpHandler());
        if (Settings.GeneralConfig.IsUseProxy ?? false)
        {
            string proxy = Settings.GeneralConfig.ProxyAddress.TrimEnd('/') + "/";
            downloadUrl = proxy + downloadUrl;
        }

        try
        {
            if (!Directory.Exists(Constants.SaveDir)) Directory.CreateDirectory(Constants.SaveDir);

            using (var response =
                   await httpClient.GetAsync(new Uri(downloadUrl), HttpCompletionOption.ResponseHeadersRead))
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = new FileStream(Path.Combine(Constants.SaveDir, Constants.SaveName), FileMode.Create))
            {
                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                long totalDownloadedByte = 0;
                var buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);

                    totalDownloadedByte += bytesRead;
                    var process = Math.Round((double)totalDownloadedByte / totalBytes * 100, 2);
                    progress?.Report(process);
                }
            }

        }
        catch (Exception ex)
        {
            Logger.Error($"下载失败：{ex}");
        }
        finally
        {
            httpClient.Dispose();
        }
    }

    private void InstallUpdate()
    {
        Logger.Info("Starting update installation...");
        try
        {
            const string updateFolder = "Update";
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string updatePath = Path.Combine(basePath, updateFolder);

            string GetPath(string fileName) => Path.Combine(basePath, fileName);
            string GetCachePath(string fileName) => Path.Combine(updatePath, fileName);

            string[] requiredFiles = ["UpdateLauncher.exe", "update.zip"];

            if (requiredFiles.All(file => File.Exists(GetPath(file))))
            {
                Directory.CreateDirectory(updatePath);

                // 复制所有文件，确保全部复制完成后再继续
                foreach (var file in requiredFiles)
                {
                    string source = GetPath(file);
                    string destination = GetCachePath(file);
                    File.Copy(source, destination, true);
                }

                // 确保所有文件都已复制后再执行更新
                CommonUtil.ExecuteProgram(GetCachePath("UpdateLauncher.exe"), [NewVersion]);
            }
            else
            {
                Logger.Warn("Missing required update files. Installation aborted.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"安装失败：{ex}");
        }
    }

    [RelayCommand]
    private async Task GoToUpdateAsync()
    {
        try
        {
            await Task.Run(async () =>
            {
                Logger.Warn("正在下载安装更新...");
                await Download(DownloadUrl);
                InstallUpdate();
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"更新失败: {ex}");
        }
    }

    [RelayCommand]
    private async Task GetReleaseNotesAsync()
    {
        ContentDialog dialog = new ContentDialog()
        {
            Title = "ReleaseNotes".GetLocalized(),
            CloseButtonText = "Close".GetLocalized(),
            Content = new ScrollViewer
            {
                Content = new TextBlock
                {
                    Text = ChangeLog,
                    Margin = new Thickness(10)
                },
                Margin = new Thickness(10)
            },
            Margin = new Thickness(10),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = App.MainWindow.Content.XamlRoot,
            RequestedTheme = _themeSelectorService.Theme
        };

        await dialog.ShowAsync();
    }
}
